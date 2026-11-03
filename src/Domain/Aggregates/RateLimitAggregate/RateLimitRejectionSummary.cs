namespace Domain.Aggregates.RateLimitAggregate;

public sealed class RateLimitRejectionSummary
{
    public string BucketKey { get; set; } = string.Empty;

    public DateTime Minute { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public string Application { get; set; } = string.Empty;

    public string Environment { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public long RejectedRequests { get; set; }

    public DateTime FirstRejectedAt { get; set; }

    public DateTime LastRejectedAt { get; set; }
}
