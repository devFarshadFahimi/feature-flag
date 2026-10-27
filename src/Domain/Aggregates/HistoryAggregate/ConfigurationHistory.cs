namespace Domain.Aggregates.HistoryAggregate;

public sealed class ConfigurationHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? ProjectId
    {
        get; set;
    }

    public Guid? EnvironmentId
    {
        get; set;
    }

    public Guid? ActorId
    {
        get; set;
    }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string ResourceType { get; set; } = string.Empty;

    public string ResourceId { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public long Revision
    {
        get; set;
    }

    public string? Before
    {
        get; set;
    }

    public string? After
    {
        get; set;
    }
}