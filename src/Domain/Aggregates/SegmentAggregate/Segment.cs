using Domain.Aggregates.SegmentAggregate.Events;

namespace Domain.Aggregates.SegmentAggregate;

public sealed class Segment : AggregateRoot<int>
{
    private Segment()
    {
    }

    public long Revision
    {
        get; private set;
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description
    {
        get; private set;
    }

    public bool IsPublic
    {
        get; private set;
    }

    public Guid? ProjectId
    {
        get; private set;
    }

    public DateTime CreatedAt
    {
        get; private set;
    }

    public DateTime? LastUsedAt
    {
        get; private set;
    }

    public List<Constraint> Constraints { get; private set; } = [];

    public static Segment Create(string name, string? description = null, IEnumerable<Constraint>? constraints = null, bool isPublic = true)
    {
        var segment = new Segment
        {
            Id = 0, // Will be assigned by DB
            Name = name,
            Description = description,
            IsPublic = isPublic,
            CreatedAt = DateTime.UtcNow
        };

        if (constraints != null)
        {
            segment.Constraints.AddRange(constraints);
        }

        segment.AddEvent(new SegmentCreatedEvent(segment.Id, name));

        return segment;
    }

    /// <summary>Recreates a deleted segment identity while retaining its revision lineage.</summary>
    /// <param name="id">The original identifier of the deleted segment.</param>
    /// <param name="projectId">The identifier of the owning project.</param>
    /// <param name="name">The restored segment's name.</param>
    /// <param name="description">The optional restored segment description.</param>
    /// <param name="isPublic">Whether the restored segment is marked public.</param>
    /// <param name="revision">The previous revision from which the restored segment continues.</param>
    /// <returns>The recreated segment with its original identifier and continued revision lineage.</returns>
    public static Segment RestoreDeleted(int id, Guid? projectId, string name, string? description, bool isPublic, long revision)
    {
        var segment = Create(name, description, isPublic: isPublic);

        segment.Id = id;
        segment.ProjectId = projectId;
        segment.Revision = revision;

        return segment;
    }

    public void AssignProject(Guid? projectId) => ProjectId = projectId;

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void AddConstraint(Constraint constraint)
    {
        Constraints.Add(constraint);
        Revision++;
    }

    public void RemoveConstraint(int index)
    {
        if (index < 0 || index >= Constraints.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "شماره قید معتبر نیست.");
        }

        Constraints.RemoveAt(index);
        Revision++;
    }

    public void UpdateConstraints(IEnumerable<Constraint> constraints)
    {
        Constraints.Clear();
        Constraints.AddRange(constraints);
        Revision++;
    }

    public void RestoreConfiguration(string name, string? description, bool isPublic, IEnumerable<Constraint> constraints)
    {
        Update(name, description);
        IsPublic = isPublic;
        UpdateConstraints(constraints);
    }

    public void MarkAsUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }
}
