using System.IO;
using System.Threading.Tasks;

namespace BitClient.HostedServices
{
    public interface IUploadService
    {
        Task<UploadResult> UploadImageAsync(Stream stream, string filename, string container);
        Task<UploadResult> GetUploadedImageAsync(string savedFilename, string container, string missingFileReplacer = null);
        UploadResult GetUploadedImage(string savedFilename, string container, string missingFileReplacer = null);
    }

    public class UploadResult
    {
        public string OriginalFileName { get; set; }
        public string SavedFileName { get; set; }
        public string DownloadURL { get; set; }
        public string FullSavePath { get; set; }
        public string Container { get; set; }
    }
}
