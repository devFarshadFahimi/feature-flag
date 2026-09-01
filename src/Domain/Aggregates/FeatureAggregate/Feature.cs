using Domain.Aggregates.FeatureAggregate.Events;

namespace Domain.Aggregates.FeatureAggregate;

public sealed class Feature : AggregateRoot<Guid>
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; }
    public FeatureType Type { get; private set; }
    public string? Description { get; private set; }
    public FeatureLifecycle Lifecycle { get; private set; }
    public bool IsStale { get; private set; }
    public bool ImpressionDataEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ArchivedAt { get; private set; }

    private readonly List<FeatureEnvironment> _environments = [];
    public IReadOnlyCollection<FeatureEnvironment> Environments => _environments.AsReadOnly();

    private readonly List<string> _tags = [];
    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

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
            Lifecycle = FeatureLifecycle.Planned,
            CreatedAt = DateTime.UtcNow
        };

        feature.Apply(new FeatureCreatedEvent(feature.Id, projectId, name));
        return feature;
    }

    public void InitializeEnvironment(Guid environmentId, IEnumerable<Variant>? defaultVariants = null)
    {
        if (_environments.Any(e => e.EnvironmentId == environmentId))
        {
            throw new InvalidEntityStateException($"Environment {environmentId} already initialized");
        }

        var env = FeatureEnvironment.Create(Id, environmentId, defaultVariants);
        _environments.Add(env);
    }

    public FeatureEnvironment GetEnvironment(Guid environmentId) =>
        _environments.FirstOrDefault(e => e.EnvironmentId == environmentId)
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

        Apply(new FeatureUpdatedEvent(Id, ProjectId, Name));
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
        Lifecycle = FeatureLifecycle.Active;
    }

    public void Complete()
    {
        Lifecycle = FeatureLifecycle.Completed;
    }

    public void Archive()
    {
        Lifecycle = FeatureLifecycle.Archived;
        ArchivedAt = DateTime.UtcNow;
        Apply(new FeatureArchivedEvent(Id, ProjectId));
    }

    public void AddTag(string tag)
    {
        if (!_tags.Contains(tag))
        {
            _tags.Add(tag);
        }
    }

    public void RemoveTag(string tag)
    {
        _ = _tags.Remove(tag);
    }
}

public sealed class FeatureEnvironment : Entity<Guid>
{
    public Guid FeatureId { get; private set; }
    public Guid EnvironmentId { get; private set; }
    public bool Enabled { get; private set; }
    public DateTime? LastSeenAt { get; private set; }

    private readonly List<Variant> _variants = [];
    public IReadOnlyCollection<Variant> Variants => _variants.AsReadOnly();

    private readonly List<FeatureStrategy> _strategies = [];
    public IReadOnlyCollection<FeatureStrategy> Strategies => _strategies.AsReadOnly();

    private FeatureEnvironment()
    {
    }

    public static FeatureEnvironment Create(Guid featureId, Guid environmentId, IEnumerable<Variant>? defaultVariants = null)
    {
        var env = new FeatureEnvironment
        {
            Id = Guid.NewGuid(),
            FeatureId = featureId,
            EnvironmentId = environmentId,
            Enabled = false
        };

        if (defaultVariants != null)
        {
            env._variants.AddRange(defaultVariants);
        }

        return env;
    }

    public void Enable()
    {
        Enabled = true;
        LastSeenAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        Enabled = false;
    }

    public void UpdateLastSeen()
    {
        LastSeenAt = DateTime.UtcNow;
    }

    public FeatureStrategy AddStrategy(StrategyType type, StrategyParameters parameters, IEnumerable<Constraint>? constraints = null, IEnumerable<int>? segmentIds = null)
    {
        var strategy = FeatureStrategy.Create(FeatureId, EnvironmentId, type, parameters, constraints, segmentIds);
        _strategies.Add(strategy);
        return strategy;
    }

    public void RemoveStrategy(Guid strategyId)
    {
        var strategy = _strategies.FirstOrDefault(s => s.Id == strategyId)
            ?? throw new InvalidEntityStateException($"Strategy {strategyId} not found");

        _ = _strategies.Remove(strategy);
    }

    public void SetVariants(IEnumerable<Variant> variants)
    {
        _variants.Clear();
        _variants.AddRange(variants);
    }
}

public sealed class FeatureStrategy : Entity<Guid>
{
    public Guid FeatureId { get; private set; }
    public Guid EnvironmentId { get; private set; }
    public StrategyType Type { get; private set; }
    public StrategyParameters Parameters { get; private set; }
    public int SortOrder { get; private set; }

    private readonly List<Constraint> _constraints = [];
    public IReadOnlyCollection<Constraint> Constraints => _constraints.AsReadOnly();

    private readonly List<int> _segmentIds = [];
    public IReadOnlyCollection<int> SegmentIds => _segmentIds.AsReadOnly();

    private readonly List<Variant> _variants = [];
    public IReadOnlyCollection<Variant> Variants => _variants.AsReadOnly();

    private FeatureStrategy()
    {
    }

    public static FeatureStrategy Create(
        Guid featureId,
        Guid environmentId,
        StrategyType type,
        StrategyParameters parameters,
        IEnumerable<Constraint>? constraints = null,
        IEnumerable<int>? segmentIds = null)
    {
        var strategy = new FeatureStrategy
        {
            Id = Guid.NewGuid(),
            FeatureId = featureId,
            EnvironmentId = environmentId,
            Type = type,
            Parameters = parameters
        };

        if (constraints != null)
        {
            strategy._constraints.AddRange(constraints);
        }

        if (segmentIds != null)
        {
            strategy._segmentIds.AddRange(segmentIds);
        }

        return strategy;
    }

    public void UpdateParameters(StrategyParameters parameters)
    {
        Parameters = parameters;
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

    public void AddSegment(int segmentId)
    {
        if (!_segmentIds.Contains(segmentId))
        {
            _segmentIds.Add(segmentId);
        }
    }

    public void RemoveSegment(int segmentId)
    {
        _ = _segmentIds.Remove(segmentId);
    }

    public void SetVariants(IEnumerable<Variant> variants)
    {
        _variants.Clear();
        _variants.AddRange(variants);
    }

    public void UpdateSortOrder(int order)
    {
        SortOrder = order;
    }
}