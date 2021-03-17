using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BitClient.HostedServices
{
    public class HostedWebClientServices : IHostedService
    {
        private readonly ILogger<HostedWebClientServices> _logger;
        private readonly IBitClientProcessor bitClientProcessor;

        public HostedWebClientServices(ILogger<HostedWebClientServices> logger, IBitClientProcessor bitClientProcessor)
        {
            this._logger = logger;
            this.bitClientProcessor = bitClientProcessor;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Initializing Persistance Services...");
                await bitClientProcessor.StartServiceAync();
                //Logg
                _logger.LogInformation("All Persistance Services started....");

            }
            catch (Exception ex)
            {
                this._logger.LogError(ex.Message);
            }
        }



        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Stopping Persistance Services...");
                await bitClientProcessor.StopServiceAsync();
                _logger.LogInformation("All Persistance Services Stopped....");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex.Message);
            }

        }
    }
}
