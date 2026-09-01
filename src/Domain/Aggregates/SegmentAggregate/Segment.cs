using Domain.Aggregates.SegmentAggregate.Events;

namespace Domain.Aggregates.SegmentAggregate;

public sealed class Segment : AggregateRoot<int>
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsPublic { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private readonly List<Constraint> _constraints = [];
    public IReadOnlyCollection<Constraint> Constraints => _constraints.AsReadOnly();



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
            segment._constraints.AddRange(constraints);
        }

        segment.Apply(new SegmentCreatedEvent(segment.Id, name));
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
        _constraints.Add(constraint);
    }

    public void RemoveConstraint(int index)
    {
        if (index < 0 || index >= _constraints.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        _constraints.RemoveAt(index);
    }

    public void UpdateConstraints(IEnumerable<Constraint> constraints)
    {
        _constraints.Clear();
        _constraints.AddRange(constraints);
    }

    public void MarkAsUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }
}