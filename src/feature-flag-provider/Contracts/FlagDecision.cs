namespace FeatureFlag.Provider.Contracts;

public sealed record FlagDecision(
    string Name,
    bool Enabled,
    EvaluatedVariant Variant,
    string Reason,
    long Revision,
    Guid? MatchedStrategyId = null);
