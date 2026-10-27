using Domain.Aggregates.SegmentAggregate.Events;

namespace Domain.Aggregates.SegmentAggregate;

public sealed class Segment : AggregateRoot<int>
{
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

    public void AssignProject(Guid? projectId) => ProjectId = projectId;

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

    private Segment()
    {
    }

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

    public void MarkAsUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }
}