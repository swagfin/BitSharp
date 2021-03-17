using BitClient.HostedServices;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace BitClient.Pages
{
    public class TrackTorrentsModel : PageModel
    {
        private readonly HostedRepository _hostedRepository;

        public List<UserTorrentManager> TorrentRepos { get; set; }
        public List<BitClientProcessorQueue> QueuesRepos { get; set; }

        public string FilteredTrackingId { get; set; }
        public TrackTorrentsModel(HostedRepository hostedRepository, ILogger<UploadTorrentFileModel> logger)
        {
            this._hostedRepository = hostedRepository;
        }


        public void OnGet(string trackingId = null)
        {
            this.TorrentRepos = _hostedRepository.UserTorrentManagers.ToList();
            this.QueuesRepos = _hostedRepository.BitClientProcessorQueues.ToList();

            if (!string.IsNullOrWhiteSpace(trackingId))
            {
                FilteredTrackingId = trackingId;
                this.TorrentRepos = TorrentRepos.Where(x => x.TrackingId == trackingId).ToList();
                this.QueuesRepos = QueuesRepos.Where(x => x.TrackingId == trackingId).ToList();
            }
        }
    }
}
