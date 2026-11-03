using System.Security.Cryptography;
using System.Text;
using BusinessMakerFramework.Endpoint.WebApi.Middlewares.RateLimits.Extensions;
using BusinessMakerFramework.Shared.Abstractions.Caching;
using BusinessMakerFramework.Shared.Extensions.Caching.Distributed.Redis.DependencyInjections;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.RateLimiting;

public static class RateLimitConfiguration
{
    private const string RedisConnectionKey = "Redis:ConnectionString";
    private const int RedisTimeoutMilliseconds = 1000;
    private static readonly TimeSpan RejectedIpRetention = TimeSpan.FromMinutes(15);

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
        builder.Services.AddSingleton(RateLimitEndpointConfiguration.Read(builder.Configuration));
        var history = builder.Configuration
            .GetSection(RateLimitHistorySettings.SectionName)
            .Get<RateLimitHistorySettings>() ?? new();

        if (!history.IsValid())
        {
            throw new InvalidOperationException("تنظیمات تاریخچه محدودسازی درخواست‌ها معتبر نیست.");
        }

        builder.Services.AddSingleton(history);

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
        _ = builder.Services.AddBuildingBlockRedisDatabase(options => options.Configuration = redisOptions.ToString());
        _ = builder.Services.AddRedisRateLimiting(options =>
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

        _ = app.Use(async (context, next) =>
        {
            await next(context);

            if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
            {
                await RecordRejectedIpAsync(context, app, settings.KeyPrefix);
            }
        });

        app.UseRedisFixedWindowRateLimit();
        app.UseRedisSlidingWindowRateLimit();
        app.UseRedisTokenBucketRateLimit();
        app.UseRedisConcurrentRateLimit();
    }

    /// <summary>Records rejected source IPs separately from limiter state; diagnostic failures never change admission or its response.</summary>
    private static async Task RecordRejectedIpAsync(HttpContext context, WebApplication app, string prefix)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        deadline.CancelAfter(TimeSpan.FromSeconds(1));

        try
        {
            var address = context.Connection.RemoteIpAddress;
            var ip = address is { IsIPv4MappedToIPv6: true } ? address.MapToIPv4().ToString() : address?.ToString() ?? "unknown";
            var route = (context.GetEndpoint() as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern.RawText ?? "unknown";
            var identity = System.Text.Json.JsonSerializer.Serialize(new[] { app.Environment.ApplicationName, app.Environment.EnvironmentName, context.Request.Method, route });
            var routeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
            var key = $"{prefix}:rejected-ips:{Uri.EscapeDataString(ip)}:{routeHash}";
            var cache = context.RequestServices.GetRequiredService<IRedisDatabaseCacheAdapter>();
            var detail = new RejectedIpDetail(ip, app.Environment.ApplicationName, app.Environment.EnvironmentName, context.Request.Method, route, DateTimeOffset.UtcNow);
            var history = context.RequestServices.GetRequiredService<RateLimitHistorySettings>();
            var redis = context.RequestServices.GetRequiredService<IConnectionMultiplexer>();

            await RateLimitHistory.EnqueueAsync(redis.GetDatabase(), prefix,
                new RateLimitHistory.RejectedRequest(ip, detail.Application, detail.Environment, detail.Method, detail.Route, detail.LastRejectedAt.UtcDateTime), history, app.Logger, deadline.Token);

            await cache.IncrementAsync(key + ":count", expiry: RejectedIpRetention, ct: deadline.Token);
            await cache.SetAsync(key + ":details", detail, RejectedIpRetention, deadline.Token);
        }
        catch (Exception exception)
        {
            app.Logger.LogWarning(exception, "ثبت اطلاعات نشانی درخواست محدودشده در ردیس انجام نشد.");
        }
    }

    private sealed record RejectedIpDetail(string IpAddress, string Application, string Environment, string Method, string Route, DateTimeOffset LastRejectedAt);
}

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimits";

    public string KeyPrefix { get; set; } = "feature-flag";

    public bool Enabled { get; set; } = true;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(KeyPrefix)
            && KeyPrefix.Length <= 64
            && KeyPrefix.All(character => char.IsAsciiLetterOrDigit(character) || character is ':' or '-' or '_');
    }
}