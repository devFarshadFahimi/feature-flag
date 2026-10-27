namespace Domain.Aggregates.ProjectAggregate;

public sealed class ProjectEnvironmentPolicy
{
    public Guid ProjectId
    {
        get; set;
    }

    public Guid EnvironmentId
    {
        get; set;
    }

    public long Revision
    {
        get; set;
    }

    public bool Enabled
    {
        get; set;
    }

    public int RequiredApprovals { get; set; } = 1;
}