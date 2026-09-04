namespace Domain.Aggregates.ChangeRequestAggregate;

public sealed class ChangeRequestItem : Entity<Guid>
{
    public Guid ChangeRequestId { get; private set; }
    public ChangeRequest ChangeRequest { get; private set; } = null!;


    public string Action { get; private set; }
    public Guid? FeatureId { get; private set; }
    public string? Payload { get; private set; }

    private ChangeRequestItem()
    {
    }

    public static ChangeRequestItem Create(string action, Guid? featureId = null, string? payload = null)
    {
        return new ChangeRequestItem
        {
            Id = Guid.NewGuid(),
            Action = action,
            FeatureId = featureId,
            Payload = payload
        };
    }
}