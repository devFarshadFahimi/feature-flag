namespace FeatureFlag.Provider.Contracts;

public sealed record EvaluationResponse(IReadOnlyList<FlagDecision> Features, DateTimeOffset EvaluatedAt, DateTimeOffset SnapshotLoadedAt, bool Stale, int RefreshAfterSeconds, int MaxStaleSeconds)
{
    /// <summary>Gets a value indicating whether the provider replayed an identical cached decision after a transient outage. Server timestamps remain unchanged.</summary>
    public bool FromCache
    {
        get; init;
    }
}