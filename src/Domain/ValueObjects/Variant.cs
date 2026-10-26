using BusinessMakerFramework.Domain.Core.ValueObjects;

namespace Domain.ValueObjects;

public sealed class Variant : BaseValueObject<Variant>
{
    public string Name
    {
        get;
    }
    public int Weight
    {
        get;
    }
    public string? Stickiness
    {
        get;
    }
    public Dictionary<string, object> Payload
    {
        get;
    }
    public bool IsDefault
    {
        get;
    }

    private Variant(string name, int weight, string? stickiness, Dictionary<string, object> payload, bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("نام گونه الزامی است.");
        }

        if (weight is < 0 or > 1000)
        {
            throw new ArgumentException("وزن باید بین صفر و ۱۰۰۰ باشد.");
        }


        Name = name;
        Weight = weight;
        Stickiness = stickiness;

        if (System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(payload).Length > 16384)
        {
            throw new ArgumentException("اندازه داده گونه بیش از حد مجاز است.");
        }


        Payload = new Dictionary<string, object>(payload);
        IsDefault = isDefault;
    }

    public static Variant Create(string name, int weight, Dictionary<string, object>? payload = null, string? stickiness = null, bool isDefault = false) => new(name, weight, stickiness, payload ?? [], isDefault);
    public static void ValidateSet(IReadOnlyList<Variant> variants)
    {
        if (variants.Count == 0)
        {
            return;
        }

        if (variants
            .Select(v => v.Name)
            .Distinct(StringComparer.Ordinal)
            .Count() != variants.Count || variants.Sum(v => (long)v.Weight) != 1000 || variants
            .Select(v => v.Stickiness)
            .Distinct(StringComparer.Ordinal)
            .Count() > 1)
        {
            throw new ArgumentException("نام گونه‌ها باید یکتا، کلید پایداری یکسان و مجموع وزن‌ها ۱۰۰۰ باشد.");
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Weight;
        yield return Stickiness ?? string.Empty;
        yield return IsDefault;
        yield return System.Text.Json.JsonSerializer.Serialize(Payload.OrderBy(p => p.Key, StringComparer.Ordinal));
    }
}