using System;

namespace BitClient.HostedServices
{
    public class BitClientProcessorQueue
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public ExecutionStatus ExecutionStatus { get; set; } = ExecutionStatus.Queued;
        public long ExecutionInMiliseconds { get; set; } = 0;
        public DateTime LastUpdatedTime { get; set; } = DateTime.UtcNow;
        public DateTime InsertionTime { get; set; } = DateTime.UtcNow;
        public string ExecutionFeedBack { get; set; }


        public string TorrentFilePath { get; set; }
        public string TorrentSavePath { get; set; }
        public string UserId { get; set; } = "Public";
    }

    public enum ExecutionStatus
    {
        Queued,
        Seeding,
        Processed,
        ErrorOccurred
    }
}
