namespace FeatureFlag.Provider.Contracts;

public sealed record EvaluationResponse(
    IReadOnlyList<FlagDecision> Features,
    DateTimeOffset EvaluatedAt,
    DateTimeOffset SnapshotLoadedAt,
    bool Stale,
    int RefreshAfterSeconds,
    int MaxStaleSeconds);
