namespace FeatureFlag.Provider.Contracts;

public sealed record EvaluationContext(
    string? UserId = null,
    string? SessionId = null,
    string? RemoteAddress = null,
    string? ApplicationHostname = null,
    DateTimeOffset? CurrentTime = null,
    Dictionary<string, string>? Properties = null);
