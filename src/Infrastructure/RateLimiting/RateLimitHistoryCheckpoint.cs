namespace Infrastructure.RateLimiting;

// Transport progress belongs to Infrastructure, independently of the reportable domain summaries.
public sealed class RateLimitHistoryCheckpoint
{
    public string StreamKey { get; set; } = string.Empty;

    public string LastStreamId { get; set; } = "0-0";

    public DateTime LastProcessedAt { get; set; }

    public long DiscardedEntries { get; set; }
}
