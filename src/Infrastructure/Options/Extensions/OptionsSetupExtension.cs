using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Options.Extensions;

public static class OptionsSetupExtension
{
    public static IServiceCollection AddOptionsSetups(this IServiceCollection services)
    {
        _ = services.ConfigureOptions<AuthSettingOptionsSetup>();
        return services;
    }
}
