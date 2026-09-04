namespace Domain.Aggregates.FeatureAggregate;

public sealed class FeatureStrategy : Entity<Guid>
{
    public StrategyType Type { get; private set; }
    public StrategyParameters Parameters { get; private set; }
    public int SortOrder { get; private set; }


    public Guid EnvironmentId { get; private set; }
    public Environment Environment { get; set; } = null!;


    public Guid FeatureEnvironmentId { get; set; }
    public FeatureEnvironment FeatureEnvironment { get; set; } = null!;

    public Guid FeatureId { get; private set; }
    public Feature Feature { get; set; } = null!;

    public List<Constraint> Constraints { get; private set; } = [];
    public List<int> SegmentIds { get; private set; } = [];
    public ICollection<Variant> Variants { get; private set; } = [];

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
            strategy.Constraints.AddRange(constraints);
        }

        if (segmentIds != null)
        {
            strategy.SegmentIds.AddRange(segmentIds);
        }

        return strategy;
    }

    public void UpdateParameters(StrategyParameters parameters)
    {
        Parameters = parameters;
    }

    public void AddConstraint(Constraint constraint)
    {
        Constraints.Add(constraint);
    }

    public void RemoveConstraint(int index)
    {
        if (index < 0 || index >= Constraints.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Constraints.RemoveAt(index);
    }

    public void AddSegment(int segmentId)
    {
        if (!SegmentIds.Contains(segmentId))
        {
            SegmentIds.Add(segmentId);
        }
    }

    public void RemoveSegment(int segmentId)
    {
        _ = SegmentIds.Remove(segmentId);
    }

    public void SetVariants(IEnumerable<Variant> variants)
    {
        Variants.Clear();
        Variants = [.. variants];
    }

    public void UpdateSortOrder(int order)
    {
        SortOrder = order;
    }
}