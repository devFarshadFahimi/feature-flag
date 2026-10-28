using FeatureFlag.Provider.Options;
using FeatureFlag.Provider.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FeatureFlag.Provider;

public static class DependencyInjection
{
    /// <summary>Registers a typed client. Resolve once per consuming flow and use that instance for login and evaluation.</summary>
    /// <param name="services">The consuming application's services.</param>
    /// <param name="configuration">The provider settings section, or a root containing the settings directly.</param>
    /// <returns>The HTTP client builder for consumer-specific transport configuration.</returns>
    public static IHttpClientBuilder AddFeatureFlagProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<FeatureFlagProviderOptions>()
            .Bind(configuration)
            .Validate(options => options.IsValid(), FeatureFlagProvider.InvalidConfigurationMessage)
            .ValidateOnStart();

        return services
            .AddHttpClient<FeatureFlagProvider>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            });
    }
}
