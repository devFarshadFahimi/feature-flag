using Domain.Aggregates.FeatureAggregate.Events;
using Domain.Aggregates.ProjectAggregate;

namespace Domain.Aggregates.FeatureAggregate;

public sealed class Feature : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public FeatureType Type { get; private set; }
    public string? Description { get; private set; }
    public FeatureLifecycle LifeCycle { get; private set; }
    public bool IsStale { get; private set; }
    public bool ImpressionDataEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ArchivedAt { get; private set; }

    public Guid ProjectId { get; private set; }
    public Project Project { get; private set; } = null!;

    public ICollection<FeatureEnvironment> Environments { get; private set; } = [];

    public List<string> Tags { get; private set; } = [];

    private Feature()
    {
    }

    public static Feature Create(Guid projectId, string name, FeatureType type, string? description = null)
    {
        var feature = new Feature
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name,
            Type = type,
            Description = description,
            LifeCycle = FeatureLifecycle.Planned,
            CreatedAt = DateTime.UtcNow
        };

        feature.AddEvent(new FeatureCreatedEvent(feature.Id, projectId, name));
        return feature;
    }

    public void InitializeEnvironment(Guid environmentId, IEnumerable<Variant>? defaultVariants = null)
    {
        if (Environments.Any(e => e.EnvironmentId == environmentId))
        {
            throw new InvalidEntityStateException($"Environment {environmentId} already initialized");
        }

        var env = FeatureEnvironment.Create(Id, environmentId, defaultVariants);
        Environments.Add(env);
    }

    public FeatureEnvironment GetEnvironment(Guid environmentId) =>
        Environments.FirstOrDefault(e => e.EnvironmentId == environmentId)
        ?? throw new InvalidEntityStateException($"Environment {environmentId} not found");

    public void Update(string? description, FeatureType? type)
    {
        if (description != null)
        {
            Description = description;
        }

        if (type.HasValue)
        {
            Type = type.Value;
        }

        AddEvent(new FeatureUpdatedEvent(Id, ProjectId, Name));
    }

    public void MarkAsStale(bool isStale)
    {
        IsStale = isStale;
    }

    public void EnableImpressionData(bool enabled)
    {
        ImpressionDataEnabled = enabled;
    }

    public void Activate()
    {
        LifeCycle = FeatureLifecycle.Active;
    }

    public void Complete()
    {
        LifeCycle = FeatureLifecycle.Completed;
    }

    public void Archive()
    {
        LifeCycle = FeatureLifecycle.Archived;
        ArchivedAt = DateTime.UtcNow;
        AddEvent(new FeatureArchivedEvent(Id, ProjectId));
    }

    public void AddTag(string tag)
    {
        if (!Tags.Contains(tag))
        {
            Tags.Add(tag);
        }
    }

    public void RemoveTag(string tag)
    {
        _ = Tags.Remove(tag);
    }
}
