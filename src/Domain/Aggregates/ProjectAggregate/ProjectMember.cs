namespace Domain.Aggregates.ProjectAggregate;

public sealed class ProjectMember : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public ProjectRole Role { get; private set; }
    public Guid ProjectId { get; private set; }
    public Project Project { get; private set; } = null!;

    public ProjectMember(Guid userId, ProjectRole role)
    {
        UserId = userId;
        Role = role;
    }
    private ProjectMember()
    {
    }

    public void UpdateRole(ProjectRole role)
    {
        Role = role;
    }
}