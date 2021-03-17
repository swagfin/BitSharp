using BitClient.HostedServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BitClient.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IBitClientProcessor _bitClientProcessor;
        public QueueProcessorStatus _queueStatus;

        public IndexModel(ILogger<IndexModel> logger, IBitClientProcessor bitClientProcessor)
        {
            _logger = logger;
            this._bitClientProcessor = bitClientProcessor;
            this._queueStatus = bitClientProcessor.GetCurrentStatus();
        }

        public async Task<ActionResult> OnGetAsync(string start = null, string stop = null)
        {
            //Stop Services
            if (!string.IsNullOrWhiteSpace(stop))
                await _bitClientProcessor.StopServiceAsync();
            //Start Services
            if (!string.IsNullOrWhiteSpace(start))
                await _bitClientProcessor.StartServiceAync();

            this._queueStatus = this._bitClientProcessor.GetCurrentStatus();
            if (!string.IsNullOrWhiteSpace(stop) || !string.IsNullOrWhiteSpace(start))
                return RedirectToAction(string.Empty);

            return Page();
        }
    }
}
