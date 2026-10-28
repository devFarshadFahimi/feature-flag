namespace FeatureFlag.Provider.Contracts;

/// <summary>Use feature names, not IDs. Null selects all flags; an empty list selects none.</summary>
public sealed record EvaluationRequest(
    Guid ProjectId,
    Guid EnvironmentId,
    string AppName,
    EvaluationContext Context,
    IReadOnlyList<string>? FeatureNames = null,
    IReadOnlyDictionary<string, bool>? Defaults = null);
