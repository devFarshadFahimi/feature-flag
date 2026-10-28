namespace FeatureFlag.Provider.Options;

public sealed class FeatureFlagProviderOptions
{
    public const string SectionName = "FeatureFlagProvider";

    public string BaseUri { get; set; } = string.Empty;

    public string AuthEndpoint { get; set; } = "Auth/login";

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
            || string.IsNullOrWhiteSpace(AuthEndpoint))
        {
            return false;
        }

        var normalizedBase = new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/");

        return Uri.TryCreate(AuthEndpoint, UriKind.Relative, out _)
            && Uri.TryCreate(normalizedBase, AuthEndpoint, out var endpoint)
            && normalizedBase.IsBaseOf(endpoint)
            && string.IsNullOrEmpty(endpoint.Query)
            && string.IsNullOrEmpty(endpoint.Fragment);
    }
}
