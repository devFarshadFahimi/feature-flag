namespace FeatureFlag.Provider.Options;

public sealed class FeatureFlagProviderOptions
{
    public const string SectionName = "FeatureFlagProvider";

    public string BaseUri { get; set; } = string.Empty;

    public string AuthEndpoint { get; set; } = "Auth/login";

    public string RefreshEndpoint { get; set; } = "Auth/refresh";

    /// <summary>Gets or sets the email address used as the username by the current login API.</summary>
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether plaintext HTTP is allowed for a loopback development endpoint.</summary>
    public bool AllowInsecureLoopback
    {
        get; set;
    }

    public int RequestTimeoutSeconds { get; set; } = 10;

    /// <summary>Gets or sets a value indicating whether identical successful evaluations can be replayed during a transient outage.</summary>
    public bool EnableLastKnownGoodFallback
    {
        get; set;
    }

    public int MaxStaleSeconds { get; set; } = 30;

    public int MaxCachedEvaluations { get; set; } = 128;

    public int MaxCacheBytes { get; set; } = 4 * 1024 * 1024;

    internal bool IsValid()
    {
        if (!Uri.TryCreate(BaseUri, UriKind.Absolute, out var baseUri) || (baseUri.Scheme != Uri.UriSchemeHttps && !(AllowInsecureLoopback && baseUri.Scheme == Uri.UriSchemeHttp && baseUri.IsLoopback)) || RequestTimeoutSeconds is < 1 or > 120 || MaxStaleSeconds is < 0 or > 3600 || MaxCachedEvaluations is < 1 or > 1024 || MaxCacheBytes is < 1024 or > 16 * 1024 * 1024 || !string.IsNullOrEmpty(baseUri.UserInfo) || !string.IsNullOrEmpty(baseUri.Query) || !string.IsNullOrEmpty(baseUri.Fragment) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(AuthEndpoint) || string.IsNullOrWhiteSpace(RefreshEndpoint))
        {
            return false;
        }

        var normalizedBase = new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/");

        return IsValidEndpoint(normalizedBase, AuthEndpoint) && IsValidEndpoint(normalizedBase, RefreshEndpoint);
    }

    private static bool IsValidEndpoint(Uri normalizedBase, string path)
    {
        return Uri.TryCreate(path, UriKind.Relative, out _) && Uri.TryCreate(normalizedBase, path, out var endpoint) && normalizedBase.IsBaseOf(endpoint) && string.IsNullOrEmpty(endpoint.Query) && string.IsNullOrEmpty(endpoint.Fragment);
    }
}