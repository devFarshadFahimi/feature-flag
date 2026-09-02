using Application.Common.Services;
using BusinessMakerFramework.Application.Service;
using BusinessMakerFramework.Infrastructure.SqlCommand.Interceptors;
using BusinessMakerFramework.Shared.Extensions.UserManagement.Extensions.DependencyInjection;
using Infrastructure.Options.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjections
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddDbContext<ApplicationDbContext>((p, options) =>
        {
            _ = options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            _ = options.AddInterceptors(p.GetRequiredService<AddAuditDataInterceptor>());
            _ = options.EnableDetailedErrors(true);
        });
        _ = services.AddFrameworkWebUserInfoService(configuration);
        _ = services.AddSingleton<AddAuditDataInterceptor>();
        _ = services.AddDataProtection().PersistKeysToDbContext<ApplicationDbContext>().SetDefaultKeyLifetime(TimeSpan.FromDays(365));
        _ = services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
        _ = services.AddOptionsSetups();
        _ = services.AddDateTimeService();

        // Settings
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();

        return services;
    }

    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await DatabaseSeeder.SeedAdminUserAsync(serviceProvider);
    }

}
