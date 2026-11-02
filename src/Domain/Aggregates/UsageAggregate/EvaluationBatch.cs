namespace Domain.Aggregates.UsageAggregate;
// Persisted before an evaluation is acknowledged; aggregation and deletion share one transaction.
public sealed class EvaluationBatch
{
    public const string PendingStatus = "Pending";

    public const string QuarantinedStatus = "Quarantined";

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProjectId
    {
        get; set;
    }

    public Guid EnvironmentId
    {
        get; set;
    }

    public string AppName { get; set; } = string.Empty;

    public string Counts { get; set; } = "[]";

    public string Status { get; set; } = PendingStatus;

    public DateTime? QuarantinedAt
    {
        get; set;
    }

    public string? FailureReason
    {
        get; set;
    }

    public DateTime RecordedAt
    {
        get; set;
    }
}