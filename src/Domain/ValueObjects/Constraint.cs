using BusinessMakerFramework.Domain.Core.ValueObjects;

namespace Domain.ValueObjects;

public sealed class Constraint : BaseValueObject<Constraint>
{
    public string ContextName { get; }
    public ConstraintOperator Operator { get; }
    public IReadOnlyList<string> Values { get; }
    public bool Inverted { get; }
    public bool CaseInsensitive { get; }

    private Constraint(string contextName, ConstraintOperator op, IEnumerable<string> values, bool inverted, bool caseInsensitive)
    {
        if (string.IsNullOrWhiteSpace(contextName))
        {
            throw new ArgumentException("Context name is required");
        }

        ContextName = contextName;
        Operator = op;
        Values = values.ToList();
        Inverted = inverted;
        CaseInsensitive = caseInsensitive;
    }

    private Constraint()
    {
    }
    public static Constraint Create(string contextName, ConstraintOperator op, IEnumerable<string> values, bool inverted = false, bool caseInsensitive = false) =>
        new(contextName, op, values, inverted, caseInsensitive);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ContextName;
        yield return (int)Operator;
        foreach (var v in Values)
        {
            yield return v;
        }

        yield return Inverted;
        yield return CaseInsensitive;
    }
}
