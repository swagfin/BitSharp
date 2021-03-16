using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
namespace BitClient.HostedServices.Implementations
{
    public class BitClientProcessor : IBitClientProcessor
    {
        private readonly ILogger Logger;
        private Timer TimerWorker { get; set; }
        private QueueProcessorStatus CurrentStatus { get; set; }
        public IServiceScopeFactory ScopeFactory { get; }
        private readonly int _timerWorkerInterval;
        private int _ExceptionCounts { get; set; } = 0;
        public SMSProcessor(IServiceScopeFactory scopeFactory, ILogger<SMSProcessor> logger, IOptions<PersistanceOptions> configs)
        {
            this.Logger = logger;
            this.CurrentStatus = QueueProcessorStatus.Stopped;
            this.ScopeFactory = scopeFactory;
            this._timerWorkerInterval = configs.Value.MessagingProcessorInterval;
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
            this.CurrentStatus = QueueProcessorStatus.Stopped;
            Logger.LogInformation("Service Stopped");
            return Task.FromResult(true);
        }

        public QueueProcessorStatus GetCurrentStatus()
        {
            return CurrentStatus;
        }

        private async void OnTimedEvent(object source, ElapsedEventArgs e)
        {

            try
            {

                TimerWorker.Stop();
                this.CurrentStatus = QueueProcessorStatus.Seeding;

                Logger.LogInformation($"Running AT: {DateTime.UtcNow} UTC");

                using (var scope = ScopeFactory.CreateScope())
                {
                    var Db = scope.ServiceProvider.GetRequiredService<CrudsoftDataContext>();
                    var allTriggers = await Db.TriggerMessagingProcessorQueues.Where(x => x.ExecutionStatus == ExecutionStatus.Queued).OrderBy(x => x.InsertionTime).Take(1000).ToListAsync();
                    if (allTriggers != null && allTriggers.Count > 0)
                        foreach (var trigger in allTriggers)
                        {
                            //Mark as Seeding
                            trigger.ExecutionStatus = ExecutionStatus.Seeding;
                            trigger.LastUpdatedTime = DateTime.UtcNow;
                            //Save and Begin Stopwatch
                            await Db.SaveChangesAsync();
                            Stopwatch stopWatch = new Stopwatch();
                            stopWatch.Start();

                            try
                            {
                                Logger.LogInformation($"Processing, Trigger :{trigger.Id} AT: {DateTime.UtcNow} UTC");

                                #region ExecutionMain
                                var msgService = scope.ServiceProvider.GetRequiredService<IMessagingService>();
                                await msgService.SendAsync(trigger.Receiver, trigger.Message);
                                #endregion

                                trigger.ExecutionFeedBack = "Processed Successfully";
                                trigger.ExecutionStatus = ExecutionStatus.Processed;
                                Logger.LogInformation($"Trigger Processed Successfully, Trigger :{trigger.Id} AT: {DateTime.UtcNow} UTC");


                            }
                            catch (Exception ex)
                            {
                                trigger.ExecutionStatus = ExecutionStatus.ErrorOccurred;
                                trigger.ExecutionFeedBack = ex.Message;
                                Logger.LogError($"ERROR: {ex.Message}, Trigger :{trigger.Id} AT: {DateTime.UtcNow} UTC");
                            }


                            //**************** DONE *********************

                            stopWatch.Stop();
                            trigger.LastUpdatedTime = DateTime.UtcNow;
                            trigger.ExecutionInMiliseconds = stopWatch.ElapsedMilliseconds;
                            await Db.SaveChangesAsync();
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

        public async Task QueueOperationAsync(SMSProcessorQueue queue)
        {
            using (var scope = ScopeFactory.CreateScope())
            {
                var Db = scope.ServiceProvider.GetRequiredService<CrudsoftDataContext>();
                await Db.TriggerMessagingProcessorQueues.AddAsync(queue);
                await Db.SaveChangesAsync();
            }
        }

        public async Task QueueOperationAsync(List<SMSProcessorQueue> queues)
        {
            using (var scope = ScopeFactory.CreateScope())
            {
                var Db = scope.ServiceProvider.GetRequiredService<CrudsoftDataContext>();
                await Db.TriggerMessagingProcessorQueues.AddRangeAsync(queues);
                await Db.SaveChangesAsync();
            }
        }
    }

}
