using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BitClient.HostedServices.Implementations
{
    public class LocalStorageUploadService : IUploadService
    {
        public IWebHostEnvironment Env { get; }
        public HttpContext Current { get; }
        public string AppBaseUrl { get { return $"{Current.Request.Scheme}://{Current.Request.Host}{Current.Request.PathBase}"; } }
        public LocalStorageUploadService(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
        {
            Env = environment;
            this.Current = httpContextAccessor.HttpContext;
        }


        public async Task<UploadResult> UploadImageAsync(Stream stream, string filename, string container)
        {
            UploadResult uploadResult = new UploadResult();
            uploadResult.Container = container;
            uploadResult.OriginalFileName = filename;
            uploadResult.SavedFileName = string.Format("{0}-{1}-{2}", Guid.NewGuid(), DateTime.Now.ToString("yyyy-MMdd-HHss"), filename);
            string folder = Path.Combine(Env.WebRootPath, container);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            uploadResult.FullSavePath = Path.Combine(folder, uploadResult.SavedFileName);
            using (stream)
                await stream.CopyToAsync(new FileStream(uploadResult.FullSavePath, FileMode.Create));
            //Generate URL
            uploadResult.DownloadURL = string.Format("{0}/{1}/{2}", this.AppBaseUrl, container, uploadResult.SavedFileName);

            return uploadResult;
        }

        public Task<UploadResult> GetUploadedImageAsync(string savedFilename, string container, string missingFileReplacer = null)
        {
            return Task.FromResult(GetUploadedImage(savedFilename, container, missingFileReplacer));
        }

        public UploadResult GetUploadedImage(string savedFilename, string container, string missingFileReplacer = null)
        {
            if (string.IsNullOrWhiteSpace(savedFilename))
                savedFilename = missingFileReplacer;
            if (string.IsNullOrWhiteSpace(container))
                container = string.Empty;
            //Proceed
            UploadResult uploadResult = new UploadResult();
            uploadResult.Container = container;
            uploadResult.OriginalFileName = "Unknown";

            string folder = Path.Combine(Env.WebRootPath, container);
            uploadResult.FullSavePath = Path.Combine(folder, savedFilename);
            if (!string.IsNullOrWhiteSpace(missingFileReplacer))
                if (!File.Exists(uploadResult.FullSavePath))
                    savedFilename = missingFileReplacer;

            uploadResult.SavedFileName = savedFilename;
            uploadResult.DownloadURL = string.Format("{0}/{1}/{2}", this.AppBaseUrl, container, savedFilename);
            return uploadResult;
        }
    }
}
