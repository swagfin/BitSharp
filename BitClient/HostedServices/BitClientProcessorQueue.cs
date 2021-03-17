using System;

namespace BitClient.HostedServices
{
    public class BitClientProcessorQueue
    {
        public string TrackingId { get; set; } = Guid.NewGuid().ToString();
        public ExecutionStatus ExecutionStatus { get; set; } = ExecutionStatus.Queued;
        public long ExecutionInMiliseconds { get; set; } = 0;
        public DateTime LastUpdatedTimeUTC { get; set; } = DateTime.UtcNow;
        public DateTime InsertionTimeUTC { get; set; } = DateTime.UtcNow;
        public string ExecutionFeedBack { get; set; }


        public byte[] TorrentFileBytes { get; set; }
        public string UserId { get; set; } = "Public";
        public int ErrorsCount { get; internal set; } = 0;
    }

    public enum ExecutionStatus
    {
        Queued,
        Seeding,
        Processed,
        ErrorOccurred
    }
}
