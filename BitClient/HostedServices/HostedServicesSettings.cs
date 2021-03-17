namespace BitClient.HostedServices
{
    public class HostedServicesSettings
    {
        public int BitClientQueueProcessorInterval { get; set; } = 1000;
        public string TorrentDownloadPath { get; set; } = "DownloadTorrents";
    }
}
