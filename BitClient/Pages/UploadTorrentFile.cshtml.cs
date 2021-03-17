using BitClient.HostedServices;
using BitClient.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace BitClient.Pages
{
    public class UploadTorrentFileModel : PageModel
    {
        public HostedRepository HostedRepository { get; }
        public IUploadService ImageUploadService { get; }
        public ILogger<UploadTorrentFileModel> Logger { get; }
        public HostedServicesSettings Options { get; }
        public string ModelErrors { get; private set; }

        public UploadTorrentFileModel(
            HostedRepository hostedRepository,
            ILogger<UploadTorrentFileModel> logger,
            IUploadService uploadService,
            IOptions<HostedServicesSettings> options)
        {
            HostedRepository = hostedRepository;
            ImageUploadService = uploadService;
            Logger = logger;
            this.Options = options.Value;
        }


        public void OnGet()
        {
        }


        public ActionResult OnPostAsync(IFormFile file)
        {
            try
            {
                if (!file.FileName.EndsWith(".torrent"))
                    throw new Exception("The File Uploaded is not a valid Torrent file( .torrent file)");
                Logger.LogInformation("Uploading File to Torrent Database");

                string userId = "demo";
                var stream = file.OpenReadStream();
                var filename = file.FileName;
                var allBytes = stream.ReadAllBytes();
                //limit max upload length
                //formFile.Length
                //No need of Uploading
                //var uploadedContent = await ImageUploadService.UploadImageAsync(stream, filename, this.Options.TorrentUploadPath + $"\\{userId}");
                //Queue For Download
                string trackingId = $"{userId}-TRACK-RA{new Random().Next(9999, 99999)}-{DateTime.Now:yyyyMMddHHmmss}".ToUpper();
                this.HostedRepository.BitClientProcessorQueues.Enqueue(new BitClientProcessorQueue
                {
                    TorrentFileBytes = allBytes,
                    TrackingId = trackingId,
                    ExecutionStatus = ExecutionStatus.Queued,
                    UserId = "demo"
                });

                return Redirect($"/TrackTorrents/?trackingId={trackingId}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                this.ModelErrors = ex.Message;
                return Page();
            }
        }
    }
}
