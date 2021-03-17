using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Client.PiecePicking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
namespace BitClient.HostedServices.Implementations
{
    public class BitClientProcessor : IBitClientProcessor
    {
        private readonly ILogger Logger;
        private readonly HostedServicesSettings options;

        private Timer TimerWorker { get; set; }
        private QueueProcessorStatus CurrentStatus { get; set; }
        public IServiceScopeFactory ScopeFactory { get; }
        public IWebHostEnvironment Environment { get; }

        private readonly int _timerWorkerInterval;
        private int _ExceptionCounts { get; set; } = 0;
        private ClientEngine TorrentEngine { get; set; }
        private BanList Banlist { get; set; }
        public BitClientProcessor(IServiceScopeFactory scopeFactory, IWebHostEnvironment environment, ILogger<BitClientProcessor> logger, IOptions<HostedServicesSettings> options)
        {
            this.Logger = logger;
            this.options = options.Value;
            this.CurrentStatus = QueueProcessorStatus.Stopped;
            this.ScopeFactory = scopeFactory;
            Environment = environment;
            this._timerWorkerInterval = this.options.BitClientQueueProcessorInterval;
        }
        void SetupEngine()
        {
            EngineSettings settings = new EngineSettings();
            settings.AllowedEncryption = ChooseEncryption();

            // If both encrypted and unencrypted connections are supported, an encrypted connection will be attempted
            // first if this is true. Otherwise an unencrypted connection will be attempted first.
            settings.PreferEncryption = true;
            // Torrents will be downloaded here by default when they are registered with the engine
            settings.SavePath = GetTorrentDownloadPath();
            // The maximum upload speed is 200 kilobytes per second, or 204,800 bytes per second
            settings.MaximumUploadSpeed = 200 * 1024;

            //EndPoint
            TorrentEngine = new ClientEngine(settings);

            // Tell the engine to listen at port 6969 for incoming connections
            // engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6969));
        }
        EncryptionTypes ChooseEncryption()
        {
            EncryptionTypes encryption;
            // This completely disables connections - encrypted connections are not allowed
            // and unencrypted connections are not allowed
            encryption = EncryptionTypes.None;

            // Only unencrypted connections are allowed
            encryption = EncryptionTypes.PlainText;

            // Allow only encrypted connections
            encryption = EncryptionTypes.RC4Full | EncryptionTypes.RC4Header;

            // Allow unencrypted and encrypted connections
            encryption = EncryptionTypes.All;
            encryption = EncryptionTypes.PlainText | EncryptionTypes.RC4Full | EncryptionTypes.RC4Header;

            return encryption;
        }

        private void SetupBanlist(string banlistFilePath)
        {
            Banlist = new BanList();

            if (string.IsNullOrWhiteSpace(banlistFilePath))
                return;
            if (!File.Exists(banlistFilePath))
                return;

            // The banlist parser can parse a standard block list from peerguardian or similar services
            BanListParser parser = new BanListParser();
            IEnumerable<AddressRange> ranges = parser.Parse(File.OpenRead("banlist"));
            Banlist.AddRange(ranges);

            // Add a few IPAddress by hand
            Banlist.Add(IPAddress.Parse("12.21.12.21"));
            Banlist.Add(IPAddress.Parse("11.22.33.44"));
            Banlist.Add(IPAddress.Parse("44.55.66.77"));

            TorrentEngine.ConnectionManager.BanPeer += delegate (object o, AttemptConnectionEventArgs e)
            {
                IPAddress address;

                // The engine can raise this event simultaenously on multiple threads
                if (IPAddress.TryParse(e.Peer.ConnectionUri.Host, out address))
                {
                    lock (Banlist)
                    {
                        // If the value of e.BanPeer is true when the event completes,
                        // the connection will be closed. Otherwise it will be allowed
                        e.BanPeer = Banlist.IsBanned(address);
                    }
                }
            };
        }

        private string GetTorrentDownloadPath()
        {
            string folder = Path.Combine(Environment.WebRootPath, this.options.TorrentDownloadPath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }
        public async Task StartServiceAync()
        {
            try
            {
                //Check if Already Running
                if (this.CurrentStatus == QueueProcessorStatus.Running || this.CurrentStatus == QueueProcessorStatus.Starting || this.CurrentStatus == QueueProcessorStatus.Seeding)
                    return;

                Logger.LogInformation("Starting Service");
                this.CurrentStatus = QueueProcessorStatus.Starting;
                //Beging Engine Setup
                SetupEngine();
                SetupBanlist("banlist.txt");
                await this.TorrentEngine.StartAll();
                //Create an Instance of Timer
                TimerWorker = new Timer();
                TimerWorker.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                TimerWorker.Interval = this._timerWorkerInterval;
                TimerWorker.Enabled = true;
                this.CurrentStatus = QueueProcessorStatus.Running;
                Logger.LogInformation("Service is Running");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error Starting Service: {ex.Message}");
                await StopServiceAsync();
                throw new Exception(ex.Message);
            }

        }

        public Task StopServiceAsync()
        {
            Logger.LogInformation("Stopping Service");
            this.CurrentStatus = QueueProcessorStatus.Stopping;
            if (TimerWorker != null)
                TimerWorker.Stop();
            //Stop Client
            this.TorrentEngine.StopAllAsync();
            this.CurrentStatus = QueueProcessorStatus.Stopped;
            Logger.LogInformation("Service Stopped");
            return Task.CompletedTask;
        }

        public QueueProcessorStatus GetCurrentStatus()
        {
            return CurrentStatus;
        }

        private async void OnTimedEvent(object source, ElapsedEventArgs e)
        {

            try
            {
                if (TorrentEngine == null)
                    throw new Exception("Torrent Engine has not been started yet");

                TimerWorker.Stop();
                this.CurrentStatus = QueueProcessorStatus.Seeding;

                Logger.LogInformation($"Running AT: {DateTime.UtcNow} UTC");

                using (var scope = ScopeFactory.CreateScope())
                {
                    var Db = scope.ServiceProvider.GetRequiredService<HostedRepository>();
                    var allQueues = Db.BitClientProcessorQueues.Where(x => x.ExecutionStatus == ExecutionStatus.Queued).ToList();
                    if (allQueues != null && allQueues.Count > 0)
                        foreach (var queue in allQueues)
                        {
                            //Mark as Seeding
                            queue.ExecutionStatus = ExecutionStatus.Seeding;
                            queue.LastUpdatedTimeUTC = DateTime.UtcNow;
                            Stopwatch stopWatch = new Stopwatch();
                            stopWatch.Start();

                            try
                            {
                                Logger.LogInformation($"Processing, Queue Tracking ID:{queue.TrackingId} AT: {DateTime.UtcNow} UTC");

                                #region ExecutionMain
                                // Load a .torrent file into memory
                                if (queue.TorrentFileBytes == null)
                                    throw new Exception("Torrent Content in Bytes is Empty");
                                Torrent torrent = await Torrent.LoadAsync(queue.TorrentFileBytes);
                                // Set all the files to not download
                                //foreach (TorrentFile file in torrent.Files)
                                //    file.Priority = Priority.High;
                                ////Set First File Prioroty
                                //torrent.Files[1].Priority = Priority.Highest;
                                string subPath = $"\\{queue.UserId}\\{queue.TrackingId}".ToUpper();
                                string saveDirectory = GetTorrentDownloadPath() + subPath;
                                UserTorrentManager manager = new UserTorrentManager(torrent, saveDirectory, new TorrentSettings());
                                //Asssign
                                manager.UserId = queue.UserId;
                                manager.TrackingId = queue.TrackingId;
                                manager.AvailableDownloadPath = $"{this.options.TorrentDownloadPath}{subPath}";

                                Db.UserTorrentManagers.Enqueue(manager);
                                await TorrentEngine.Register(manager);
                                // Disable rarest first and randomised picking - only allow priority based picking (i.e. selective downloading)
                                PiecePicker picker = new StandardPicker();
                                picker = new PriorityPicker(picker);
                                await manager.ChangePickerAsync(picker);

                                await this.TorrentEngine.StartAll();
                                #endregion

                                queue.ExecutionFeedBack = "Torrent added to Queue Successfully";
                                queue.ExecutionStatus = ExecutionStatus.Processed;
                                Logger.LogInformation($"Torrent added to Queue Successfully, Tracking Id :{queue.TrackingId} AT: {DateTime.UtcNow} UTC");


                            }
                            catch (Exception ex)
                            {
                                queue.ErrorsCount++;
                                if (queue.ErrorsCount >= 10)
                                {
                                    queue.ExecutionStatus = ExecutionStatus.ErrorOccurred;
                                    queue.ExecutionFeedBack = ex.Message;
                                }
                                else
                                {
                                    queue.ExecutionStatus = ExecutionStatus.Queued;
                                    queue.ExecutionFeedBack = $"Requeued Attempt ({queue.ErrorsCount})";
                                }

                                Logger.LogError($"ERROR: {ex.Message}, Tracking Id :{queue.TrackingId} AT: {DateTime.UtcNow} UTC");
                            }


                            //**************** DONE *********************

                            stopWatch.Stop();
                            queue.LastUpdatedTimeUTC = DateTime.UtcNow;
                            queue.ExecutionInMiliseconds = stopWatch.ElapsedMilliseconds;
                        }


                    //Exception Count Reset
                    _ExceptionCounts = 0;

                }

            }
            catch (Exception ex)
            {
                _ExceptionCounts++;
                Logger.LogError($"Processing Exception: {ex.Message}");
            }
            finally
            {
                ContinueTimer();
            }
        }

        private void ContinueTimer()
        {
            try
            {
                //Check if Exceess Errors
                if (_ExceptionCounts >= 60)
                {
                    Logger.LogWarning("Terminating Service Due to Exceeding Error Counts");
                    this.CurrentStatus = QueueProcessorStatus.Stopping;
                    StopServiceAsync();
                    return;
                }

                if (TimerWorker == null)
                    return;
                if (this.CurrentStatus == QueueProcessorStatus.Stopped || this.CurrentStatus == QueueProcessorStatus.Stopping)
                    return;
                //Continue Timer
                TimerWorker.Start();
                this.CurrentStatus = QueueProcessorStatus.Running;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }
        }

    }
}
