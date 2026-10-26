namespace Domain.Aggregates.UsageAggregate;

public sealed class FlagUsage
{
    public Guid ProjectId
    {
        get; set;
    }
    public Guid EnvironmentId
    {
        get; set;
    }
    public string AppName { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public bool Enabled
    {
        get; set;
    }
    public bool Impression
    {
        get; set;
    }
    public DateTime Hour
    {
        get; set;
    }
    public DateTime LastSeenAt
    {
        get; set;
    }
    public long Count
    {
        get; set;
    }
}

public sealed class ImpressionBatch
{
    public Guid ProjectId
    {
        get; set;
    }
    public Guid EnvironmentId
    {
        get; set;
    }
    public Guid BatchId
    {
        get; set;
    }
    public DateTime ReceivedAt
    {
        get; set;
    }
}