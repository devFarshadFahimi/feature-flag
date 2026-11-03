using System.Text.RegularExpressions;
using BusinessMakerFramework.Endpoint.WebApi.Middlewares.RateLimits.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.RateLimiting;

public static class RateLimitEndpointConfiguration
{
    private const string FixedWindowSection = "FixedWindowRateLimit";
    private const string SlidingWindowSection = "SlidingWindowRateLimit";
    private const string TokenBucketSection = "TokenBucketRateLimit";
    private const string ConcurrencySection = "ConcurrencyRateLimit";
    private const string PathField = "path";
    private const string MethodsField = "methods";
    private const string LimitField = "limit";
    private const string WindowSecondsField = "windowSeconds";
    private const string CapacityField = "capacity";
    private const string RefillRateField = "refillRate";
    private const string MaxConcurrentRequestsField = "maxConcurrentRequests";

    /// <summary>Converts route configuration to framework attributes. Explicit action/controller attributes take precedence for the same limiter type.</summary>
    /// <param name = "app">The application whose controllers are mapped.</param>
    public static void MapRateLimitedControllers(this WebApplication app)
    {
        var configuration = app.Services.GetRequiredService<ConfiguredRateLimits>();
        var settings = app.Services.GetRequiredService<RateLimitSettings>();

        app
            .MapControllers()
            .Add(endpoint =>
        {
            var action = endpoint.Metadata
                .OfType<ControllerActionDescriptor>()
                .SingleOrDefault();

            if (action is null || !settings.Enabled)
            {
                return;
            }

            var methods = endpoint.Metadata
                .OfType<HttpMethodMetadata>()
                .SelectMany(metadata => metadata.HttpMethods)
                .ToArray();
            var path = Normalize(action.AttributeRouteInfo?.Template ?? string.Empty);
            var groups = configuration.Rules
                .Where(rule => rule.Matches(path, methods))
                .GroupBy(rule => rule.Attribute.GetType());

            foreach (var group in groups)
            {
                if (endpoint.Metadata.Any(metadata => metadata.GetType() == group.Key))
                {
                    continue;
                }

                var selected = group
                    .OrderByDescending(rule => rule.Specificity)
                    .ThenByDescending(rule => rule.Methods.Length > 0)
                    .First();

                endpoint.Metadata.Add(selected.Attribute);
            }
        });

        // Materialize controller endpoints now so route typos fail startup instead of silently leaving an action unprotected.
        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Where(endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is not null)
            .ToArray();

        if (settings.Enabled && !app.Environment.IsDevelopment()
            && endpoints.Any(endpoint => endpoint.Metadata.GetMetadata<RateLimitAttributeBase>() is null))
        {
            throw new InvalidOperationException("تمام عملیات کنترلرها در محیط عملیاتی باید صفت محدودسازی یا پیکربندی مسیر داشته باشند.");
        }

        foreach (var rule in configuration.Rules)
        {
            if (!endpoints.Any(endpoint => MatchesEndpoint(rule, endpoint)))
            {
                throw new InvalidOperationException($"مسیر پیکربندی محدودسازی درخواست‌ها با هیچ عملیاتی مطابقت ندارد: {rule.Path}");
            }
        }
    }

    /// <summary>Validates configured paths and limits before producing the framework attributes; ambiguous or unknown fields fail startup.</summary>
    /// <param name = "configuration">The host configuration containing per-type route arrays.</param>
    /// <returns>The validated framework attributes and their route selectors.</returns>
    internal static ConfiguredRateLimits Read(IConfiguration configuration)
    {
        var rules = new List<ConfiguredRateLimit>();

        foreach (var sectionName in new[] { FixedWindowSection, SlidingWindowSection, TokenBucketSection, ConcurrencySection })
        {
            var entries = configuration
                .GetSection(sectionName)
                .GetChildren();

            foreach (var entry in entries)
            {
                var allowed = sectionName switch
                {
                    TokenBucketSection => new[] { PathField, MethodsField, CapacityField, RefillRateField },
                    ConcurrencySection => new[] { PathField, MethodsField, MaxConcurrentRequestsField },
                    _ => new[] { PathField, MethodsField, LimitField, WindowSecondsField }
                };
                var unknownField = entry
                    .GetChildren()
                    .Any(field => !allowed.Contains(field.Key, StringComparer.OrdinalIgnoreCase));

                if (!int.TryParse(entry.Key, out _) || unknownField || entry.GetSection(MethodsField).Value is not null)
                {
                    throw new InvalidOperationException($"ساختار یا نام فیلد پیکربندی محدودسازی معتبر نیست: {entry.Path}");
                }

                var path = entry[PathField]?.Trim().Trim('/');
                var methods = entry
                    .GetSection(MethodsField)
                    .Get<string[]>() ?? [];

                if (string.IsNullOrWhiteSpace(path) || path.Contains('?') || path.Contains('#')
                    || (path.Contains('*') && path != "*" && (!path.EndsWith("/*", StringComparison.Ordinal) || path.Count(character => character == '*') != 1))
                    || methods.Any(method => string.IsNullOrWhiteSpace(method) || !method.All(char.IsAsciiLetter)))
                {
                    throw new InvalidOperationException($"مسیر یا روش درخواست در پیکربندی محدودسازی معتبر نیست: {entry.Path}");
                }

                var attribute = sectionName switch
                {
                    FixedWindowSection => (RateLimitAttributeBase)new FixedWindowRateLimitAttribute(Positive(entry, LimitField), Positive(entry, WindowSecondsField, 3600)),
                    SlidingWindowSection => new SlidingWindowRateLimitAttribute(Positive(entry, LimitField), Positive(entry, WindowSecondsField, 3600)),
                    TokenBucketSection => new TokenBucketRateLimitAttribute(Positive(entry, CapacityField), Positive(entry, RefillRateField)),
                    _ => new ConcurrencyRateLimitAttribute(Positive(entry, MaxConcurrentRequestsField))
                };
                var rule = new ConfiguredRateLimit(Normalize(path), methods, attribute);

                if (rules.Any(existing => existing.Path.Equals(rule.Path, StringComparison.OrdinalIgnoreCase)
                    && existing.Attribute.GetType() == attribute.GetType()
                    && ((existing.Methods.Length == 0 && methods.Length == 0) || existing.Methods.Intersect(methods, StringComparer.OrdinalIgnoreCase).Any())))
                {
                    throw new InvalidOperationException($"پیکربندی تکراری یا مبهم محدودسازی درخواست‌ها وجود دارد: {entry.Path}");
                }

                rules.Add(rule);
            }
        }

        return new ConfiguredRateLimits(rules);
    }

    private static int Positive(IConfigurationSection entry, string field, int maximum = 100_000)
    {
        if (!int.TryParse(entry[field], out var value) || value <= 0 || value > maximum)
        {
            throw new InvalidOperationException($"مقدار پیکربندی محدودسازی باید بین ۱ و {maximum} باشد: {entry.Path}:{field}");
        }

        return value;
    }

    private static bool MatchesEndpoint(ConfiguredRateLimit rule, Endpoint endpoint)
    {
        var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()!;
        var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];

        return rule.Matches(Normalize(action.AttributeRouteInfo?.Template ?? string.Empty), methods);
    }

    private static string Normalize(string path)
        => Regex.Replace(path.Trim('/'), "^api/v[^/]+/", string.Empty, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    internal sealed record ConfiguredRateLimits(IReadOnlyList<ConfiguredRateLimit> Rules);

    internal sealed record ConfiguredRateLimit(string Path, string[] Methods, RateLimitAttributeBase Attribute)
    {
        internal int Specificity => Path == "*" ? 0 : Path.EndsWith("/*", StringComparison.Ordinal) ? Path.Length : 10_000 + Path.Length;

        internal bool Matches(string path, IReadOnlyList<string> methods)
        {
            var matchesPath = Path == "*"
                || Path.Equals(path, StringComparison.OrdinalIgnoreCase)
                || (Path.EndsWith("/*", StringComparison.Ordinal)
                    && (path.Equals(Path[..^2], StringComparison.OrdinalIgnoreCase) || path.StartsWith(Path[..^1], StringComparison.OrdinalIgnoreCase)));

            return matchesPath && (Methods.Length == 0 || (methods.Count > 0 && methods.All(method => Methods.Contains(method, StringComparer.OrdinalIgnoreCase))));
        }
    }
}