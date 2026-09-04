using Application.Common.Interfaces;
using Application.Common.Services;
using Application.Common.Settings;
using BusinessMakerFramework.Application.Service;
using BusinessMakerFramework.Infrastructure.SqlCommand.Interceptors;
using BusinessMakerFramework.Shared.Extensions.UserManagement.Extensions.DependencyInjection;
using Infrastructure.Options.Extensions;
using Infrastructure.Persistence.Seed;
using Infrastructure.Services;
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
        _ = services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Services
        _ = services.AddScoped<IPasswordHasher, PasswordHasher>();
        _ = services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        _ = services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();

        return services;
    }

    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await DatabaseSeeder.SeedAdminUserAsync(serviceProvider);
    }

}
