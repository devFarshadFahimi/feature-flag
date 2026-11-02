using FeatureFlag.Provider.Options;
using FeatureFlag.Provider.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FeatureFlag.Provider;

public static class DependencyInjection
{
    /// <summary>Registers one provider holding login state; each operation obtains a fresh named HTTP client.</summary>
    /// <param name = "services">The consuming application's services.</param>
    /// <param name = "configuration">The provider settings section, or a root containing the settings directly.</param>
    /// <returns>The HTTP client builder for consumer-specific transport configuration.</returns>
    public static IHttpClientBuilder AddFeatureFlagProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<FeatureFlagProviderOptions>()
            .Bind(configuration)
            .Validate(options => options.IsValid(), FeatureFlagProvider.InvalidConfigurationMessage)
            .ValidateOnStart();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton(provider => new FeatureFlagProvider(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<TimeProvider>(), provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FeatureFlagProviderOptions>>()));

        return services
            .AddHttpClient(FeatureFlagProvider.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
    }
}