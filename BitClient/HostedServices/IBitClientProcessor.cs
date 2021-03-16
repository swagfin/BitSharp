using System.Threading.Tasks;

namespace BitClient.HostedServices
{
    public interface IBitClientProcessor
    {
        Task StartServiceAync();
        Task StopServiceAsync();
        QueueProcessorStatus GetCurrentStatus();
        Task QueueOperationAsync(string torrentFilePath, string savePath);
    }
    public enum QueueProcessorStatus
    {
        Running,
        Starting,
        Stopped,
        Stopping,
        Seeding
    }
}
