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

    internal bool IsValid()
    {
        if (!Uri.TryCreate(BaseUri, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(baseUri.UserInfo)
            || !string.IsNullOrEmpty(baseUri.Query)
            || !string.IsNullOrEmpty(baseUri.Fragment)
            || string.IsNullOrWhiteSpace(Username)
            || string.IsNullOrWhiteSpace(Password)
            || string.IsNullOrWhiteSpace(AuthEndpoint)
            || string.IsNullOrWhiteSpace(RefreshEndpoint))
        {
            return false;
        }

        var normalizedBase = new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/");

        return IsValidEndpoint(normalizedBase, AuthEndpoint)
            && IsValidEndpoint(normalizedBase, RefreshEndpoint);
    }

    private static bool IsValidEndpoint(Uri normalizedBase, string path)
    {
        return Uri.TryCreate(path, UriKind.Relative, out _)
            && Uri.TryCreate(normalizedBase, path, out var endpoint)
            && normalizedBase.IsBaseOf(endpoint)
            && string.IsNullOrEmpty(endpoint.Query)
            && string.IsNullOrEmpty(endpoint.Fragment);
    }
}
