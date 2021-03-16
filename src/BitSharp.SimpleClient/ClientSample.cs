using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Client.PiecePicking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Timers;

namespace Samples
{
    public class ClientSample
    {
        BanList banlist;
        ClientEngine engine;
        List<TorrentManager> managers = new List<TorrentManager>();
        private Timer TimerWorker { get; set; }
        public ClientSample()
        {
            SetupEngine();
            //Create an Instance of Timer
            TimerWorker = new Timer();
            TimerWorker.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            TimerWorker.Interval = 1000;
            TimerWorker.Enabled = true;

        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            //Console.Clear();
            var startAt = 0;
            Console.WriteLine(Environment.NewLine);
            foreach (var manager in managers)
            {
                startAt++;
                if (manager != null)
                {

                    Console.WriteLine($"{startAt}. {manager.Torrent} | Size: {ReadableSizeDisplay(manager.Torrent.Size)} | Downloaded: {ReadableSizeDisplay(manager.Monitor.DataBytesDownloaded)}    Speed: {ReadableSpeedDisplay(manager.Monitor.DownloadSpeed)}  | Seeds: {manager.Peers.Seeds:N0} | Leachs: {manager.Peers.Leechs:N0} | Progress:  {manager.Progress:N2}% | State: {manager.State}");
                    if (manager.Error != null)
                        Console.WriteLine($"!! Error: Reason: {manager.Error.Reason}, Details: {manager.Error.Exception.Message} !!");
                    else if (manager.Complete)
                        Console.WriteLine($"ADM Download: http://files///{manager.SavePath}");
                }
            }

        }

        void SetupEngine()
        {
            EngineSettings settings = new EngineSettings();
            settings.AllowedEncryption = ChooseEncryption();

            // If both encrypted and unencrypted connections are supported, an encrypted connection will be attempted
            // first if this is true. Otherwise an unencrypted connection will be attempted first.
            settings.PreferEncryption = true;

            // Torrents will be downloaded here by default when they are registered with the engine
            settings.SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Torrents");

            // The maximum upload speed is 200 kilobytes per second, or 204,800 bytes per second
            settings.MaximumUploadSpeed = 200 * 1024;

            //EndPoint
            engine = new ClientEngine(settings);

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

        public void SetupBanlist(string banlistFilePath)
        {
            banlist = new BanList();

            if (string.IsNullOrWhiteSpace(banlistFilePath))
                return;
            if (!File.Exists(banlistFilePath))
                return;

            // The banlist parser can parse a standard block list from peerguardian or similar services
            BanListParser parser = new BanListParser();
            IEnumerable<AddressRange> ranges = parser.Parse(File.OpenRead("banlist"));
            banlist.AddRange(ranges);

            // Add a few IPAddress by hand
            banlist.Add(IPAddress.Parse("12.21.12.21"));
            banlist.Add(IPAddress.Parse("11.22.33.44"));
            banlist.Add(IPAddress.Parse("44.55.66.77"));

            engine.ConnectionManager.BanPeer += delegate (object o, AttemptConnectionEventArgs e)
            {
                IPAddress address;

                // The engine can raise this event simultaenously on multiple threads
                if (IPAddress.TryParse(e.Peer.ConnectionUri.Host, out address))
                {
                    lock (banlist)
                    {
                        // If the value of e.BanPeer is true when the event completes,
                        // the connection will be closed. Otherwise it will be allowed
                        e.BanPeer = banlist.IsBanned(address);
                    }
                }
            };
        }

        public async Task LoadTorrentAsync(string torrentFilePath, string savePath)
        {
            // Load a .torrent file into memory
            Torrent torrent = await Torrent.LoadAsync(torrentFilePath);

            // Set all the files to not download
            //foreach (TorrentFile file in torrent.Files)
            //    file.Priority = Priority.High;
            ////Set First File Prioroty
            //torrent.Files[1].Priority = Priority.Highest;
            if (string.IsNullOrWhiteSpace(savePath))
                savePath = "TorrentsDownload";
            //Proceed
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            TorrentManager manager = new TorrentManager(torrent, savePath, new TorrentSettings());
            managers.Add(manager);
            await engine.Register(manager);
            // Disable rarest first and randomised picking - only allow priority based picking (i.e. selective downloading)
            PiecePicker picker = new StandardPicker();
            picker = new PriorityPicker(picker);
            await manager.ChangePickerAsync(picker);

            await engine.StartAll();

        }


        static string ReadableSizeDisplay(Int64 value, int decimalPlaces = 2)
        {
            string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + ReadableSizeDisplay(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }

        static string ReadableSpeedDisplay(Int64 value, int decimalPlaces = 1)
        {
            string[] SizeSuffixes = { "bps", "Kbps", "MBps", "GBps", "TBps", "PBps", "EBps", "ZBps", "YBps" };
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + ReadableSpeedDisplay(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "}", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }
    }
}
