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
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("رشته اتصال پایگاه داده پیکربندی نشده است.");
        }

        _ = services.AddDbContext<ApplicationDbContext>((p, options) =>
        {
            _ = options.UseNpgsql(connectionString);
            _ = options.AddInterceptors(p.GetRequiredService<AddAuditDataInterceptor>());
            _ = options.EnableDetailedErrors(true);
        });
        _ = services.AddFrameworkWebUserInfoService(configuration);
        _ = services.AddSingleton<AddAuditDataInterceptor>();
        _ = services
            .AddDataProtection()
            .PersistKeysToDbContext<ApplicationDbContext>()
            .SetDefaultKeyLifetime(TimeSpan.FromDays(365));
        _ = services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
        _ = services.AddOptionsSetups();
        _ = services.AddDateTimeService();
        // Settings
        _ = services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        // Services
        _ = services.AddScoped<IPasswordHasher, PasswordHasher>();
        _ = services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        _ = services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        _ = services
            .AddOptions<Application.Features.Evaluations.EvaluationSettings>()
            .Bind(configuration.GetSection("Evaluation"))
            .Validate(s => s.RefreshSeconds is >= 1 and <= 60 && s.MaxStaleSeconds is >= 0 and <= 3600, "تنظیمات زمان تازه‌سازی یا نگهداری پیکربندی معتبر نیست.")
            .ValidateOnStart();
        _ = services.AddSingleton<Application.Features.Evaluations.IConfigurationSnapshots, ConfigurationSnapshots>();
        _ = services.AddHostedService<ScheduledChangeExecutor>();

        return services;
    }

    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await DatabaseSeeder.SeedAdminUserAsync(serviceProvider);
    }
}