namespace Domain.Aggregates.FeatureAggregate;

public sealed class FeatureEnvironment : Entity<Guid>
{
    public long Revision
    {
        get; private set;
    }

    public void ConfigurationChanged() => Revision++;

    public Guid EnvironmentId
    {
        get; private set;
    }

    public bool Enabled
    {
        get; private set;
    }

    public DateTime? LastSeenAt
    {
        get; private set;
    }

    public Guid FeatureId
    {
        get; set;
    }

    public Feature Feature { get; set; } = null!;

    public ICollection<Variant> Variants { get; private set; } = [];

    public ICollection<FeatureStrategy> Strategies { get; private set; } = [];

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
            env.Variants = [.. defaultVariants];
        }

        return env;
    }

    public void Enable()
    {
        Enabled = true;
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

        strategy.FeatureEnvironmentId = Id;
        Strategies.Add(strategy);

        return strategy;
    }

    public void RemoveStrategy(Guid strategyId)
    {
        var strategy = Strategies.FirstOrDefault(s => s.Id == strategyId) ?? throw new InvalidEntityStateException($"راهبرد {strategyId} یافت نشد.");

        _ = Strategies.Remove(strategy);
        ConfigurationChanged();
    }

    public void SetVariants(IEnumerable<Variant> variants)
    {
        var replacement = variants.ToList();

        Variant.ValidateSet(replacement);
        Variants.Clear();
        Variants = replacement;
    }
}