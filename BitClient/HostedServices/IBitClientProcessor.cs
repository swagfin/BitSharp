using System.Threading.Tasks;

namespace BitClient.HostedServices
{
    public interface IBitClientProcessor
    {
        Task StartServiceAync();
        Task StopServiceAsync();
        QueueProcessorStatus GetCurrentStatus();
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
