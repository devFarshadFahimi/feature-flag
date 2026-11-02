using BusinessMakerFramework.Endpoint.WebApi.Middlewares.RateLimits.Attributes;
using BusinessMakerFramework.Endpoint.WebApi.Middlewares.RateLimits.Extensions;
using BusinessMakerFramework.Shared.Extensions.Caching.Distributed.Redis.DependencyInjections;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Infrastructure.RateLimiting;

public static class RateLimitConfiguration
{
    private const string RedisConnectionKey = "Redis:ConnectionString";
    private const string AuthControllerName = "Auth";
    private const int RedisTimeoutMilliseconds = 1000;

    /// <summary>Registers the framework's atomic Redis limiter with fail-closed admission and IP partitioning.</summary>
    /// <param name = "builder">The application builder.</param>
    public static void AddFeatureFlagRateLimits(this WebApplicationBuilder builder)
    {
        var settings = builder.Configuration
            .GetSection(RateLimitSettings.SectionName)
            .Get<RateLimitSettings>() ?? new();

        if (!settings.IsValid() || (!builder.Environment.IsDevelopment() && !settings.Enabled))
        {
            throw new InvalidOperationException("تنظیمات محدودسازی درخواست‌ها معتبر نیست؛ محدودسازی در محیط عملیاتی الزامی است.");
        }

        builder.Services.AddSingleton(settings);

        if (!settings.Enabled)
        {
            return;
        }

        var connection = builder.Configuration[RedisConnectionKey];

        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException("اتصال ردیس برای محدودسازی درخواست‌ها الزامی است.");
        }

        var redisOptions = ConfigurationOptions.Parse(connection);

        redisOptions.AbortOnConnectFail = false;
        redisOptions.ConnectTimeout = RedisTimeoutMilliseconds;
        redisOptions.AsyncTimeout = RedisTimeoutMilliseconds;
        redisOptions.SyncTimeout = RedisTimeoutMilliseconds;
        redisOptions.ConnectRetry = 1;
        builder.Services.AddBuildingBlockRedisDatabase(options => options.Configuration = redisOptions.ToString());
        builder.Services.AddRedisRateLimiting(options =>
        {
            options.KeyPrefix = settings.KeyPrefix;
            options.FailOpen = false;
            options.PartitionKeySelector = context => context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        });
    }

    public static void UseFeatureFlagRateLimits(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<RateLimitSettings>();

        if (!settings.Enabled)
        {
            return;
        }

        app.UseRedisFixedWindowRateLimit();
    }

    public static void ApplyRateLimit(this EndpointBuilder endpoint, RateLimitSettings settings)
    {
        var action = endpoint.Metadata
            .OfType<ControllerActionDescriptor>()
            .SingleOrDefault();

        if (action is null)
        {
            return;
        }

        var limit = action.ControllerName switch
        {
            AuthControllerName when action.ActionName == "Login" => settings.LoginLimit,
            AuthControllerName when action.ActionName == "RefreshToken" => settings.RefreshLimit,
            "Evaluations" => settings.EvaluationLimit,
            _ => settings.ManagementLimit
        };

        endpoint.Metadata.Add(new FixedWindowRateLimitAttribute(limit, settings.WindowSeconds));
    }

}

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimits";

    public string KeyPrefix { get; set; } = "feature-flag";

    public bool Enabled { get; set; } = true;

    public int WindowSeconds { get; set; } = 60;

    public int LoginLimit { get; set; } = 10;

    public int RefreshLimit { get; set; } = 60;

    public int EvaluationLimit { get; set; } = 600;

    public int ManagementLimit { get; set; } = 120;

    public bool IsValid()
    {
        var limits = new[] { LoginLimit, RefreshLimit, EvaluationLimit, ManagementLimit };

        return !string.IsNullOrWhiteSpace(KeyPrefix)
            && KeyPrefix.Length <= 64
            && KeyPrefix.All(character => char.IsAsciiLetterOrDigit(character) || character is ':' or '-' or '_')
            && WindowSeconds is >= 1 and <= 3600
            && limits.All(limit => limit is >= 1 and <= 100_000);
    }
}
