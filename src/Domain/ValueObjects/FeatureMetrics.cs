using BusinessMakerFramework.Domain.Core.ValueObjects;

namespace Domain.ValueObjects;

public sealed class FeatureMetrics : BaseValueObject<FeatureMetrics>
{
    public int YesCount { get; }
    public int NoCount { get; }
    public Dictionary<string, int> VariantCounts { get; }
    public DateTime Timestamp { get; }

    private FeatureMetrics(int yesCount, int noCount, Dictionary<string, int> variantCounts, DateTime timestamp)
    {
        YesCount = yesCount;
        NoCount = noCount;
        VariantCounts = variantCounts;
        Timestamp = timestamp;
    }

    public static FeatureMetrics Create(int yesCount, int noCount, Dictionary<string, int>? variantCounts = null) =>
        new(yesCount, noCount, variantCounts ?? [], DateTime.UtcNow);

    public FeatureMetrics AddMetrics(FeatureMetrics other) =>
        new(
            YesCount + other.YesCount,
            NoCount + other.NoCount,
            VariantCounts.Concat(other.VariantCounts)
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Value)),
            DateTime.UtcNow
        );

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return YesCount;
        yield return NoCount;
        yield return Timestamp;
    }
}
