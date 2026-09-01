using BusinessMakerFramework.Domain.Core.ValueObjects;

namespace Domain.ValueObjects;

public sealed class Variant : BaseValueObject<Variant>
{
    public string Name { get; }
    public int Weight { get; }
    public string? Stickiness { get; }
    public Dictionary<string, object> Payload { get; }
    public bool IsDefault { get; }

    private Variant(string name, int weight, string? stickiness, Dictionary<string, object> payload, bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Variant name is required");
        }

        if (weight is < 0 or > 1000)
        {
            throw new ArgumentException("Weight must be between 0 and 1000");
        }

        Name = name;
        Weight = weight;
        Stickiness = stickiness;
        Payload = payload;
        IsDefault = isDefault;
    }

    public static Variant Create(string name, int weight, Dictionary<string, object>? payload = null, string? stickiness = null, bool isDefault = false) =>
        new(name, weight, stickiness, payload ?? [], isDefault);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Weight;
        yield return Stickiness ?? string.Empty;
        yield return IsDefault;
    }
}
