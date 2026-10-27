using Application.Common.Services;
using Application.Features.Evaluations;
using BusinessMakerFramework.Shared.Abstractions.Caching;
using Domain.Evaluation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services;
// The owner adapter supplies cache storage; this unit only bounds keys and coordinates snapshot refreshes.
public sealed class ConfigurationSnapshots(IServiceScopeFactory scopes, ICacheAdapter cache, IOptions<EvaluationSettings> options, ILogger<ConfigurationSnapshots> logger) : IConfigurationSnapshots
{
    private readonly SemaphoreSlim gate = new(1, 1);

    private readonly Dictionary<string, long> keys = new(StringComparer.Ordinal);

    private long generation;

    public void InvalidateAll()
    {
        // Versioned keys keep a transaction's invalidation from racing a concurrent in-flight load.
        Interlocked.Increment(ref generation);
    }

    /// <summary>Loads a consistent configuration snapshot, including prerequisites, with bounded stale fallback.</summary>
    public async Task<ConfigurationSnapshot> LoadAsync(Guid projectId, Guid environmentId, bool fresh, CancellationToken ct, IReadOnlyCollection<string>? featureNames = null)
    {
        await gate.WaitAsync(ct);

        try
        {
            var version = Volatile.Read(ref generation);
            var selection = featureNames is null ? "all" : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(featureNames.Order(StringComparer.Ordinal)))));
            var key = $"flags:{version}:{projectId:D}:{environmentId:D}:{selection}";
            var previous = cache.Get<ConfigurationSnapshot>(key);

            if (!fresh && previous is not null && DateTimeOffset.UtcNow - previous.LoadedAt < TimeSpan.FromSeconds(options.Value.RefreshSeconds))
            {
                return previous;
            }

            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                // A repeatable-read snapshot prevents mixing pre/post-commit strategy and segment data.
                await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
                var environment = await db.Environments
                    .AsNoTracking()
                    .SingleOrDefaultAsync(e => e.Id == environmentId, ct) ?? throw new KeyNotFoundException("محیط یافت نشد.");
                var query = db.Features
                    .AsNoTracking()
                    .Where(f => f.ProjectId == projectId)
                    .Include(f => f.Environments.Where(e => e.EnvironmentId == environmentId))
                    .ThenInclude(e => e.Strategies)
                    .ThenInclude(s => s.Constraints)
                    .Include(f => f.Environments.Where(e => e.EnvironmentId == environmentId))
                    .ThenInclude(e => e.Strategies)
                    .ThenInclude(s => s.Variants)
                    .Include(f => f.Environments.Where(e => e.EnvironmentId == environmentId))
                    .ThenInclude(e => e.Variants)
                    .AsSplitQuery();

                var features = featureNames is null ? await query
                    .Take(1001)
                    .ToListAsync(ct) : await query
                    .Where(f => featureNames.Contains(f.Name))
                    .ToListAsync(ct);

                if (featureNames is null && features.Count > 1000)
                {
                    throw new ArgumentException("پروژه بیش از سقف ارزیابی هم‌زمان پرچم دارد.");
                }

                // Fetch only the transitive prerequisite closure for explicitly requested batches.
                var loadedIds = features
                    .Select(f => f.Id)
                    .ToHashSet();

                for (var depth = 0; depth < 20; depth++)
                {
                    var missingIds = features
                        .SelectMany(f => f.Prerequisites)
                        .Select(p => p.FeatureId)
                        .Where(id => !loadedIds.Contains(id))
                        .Distinct()
                        .ToArray();

                    if (missingIds.Length == 0)
                    {
                        break;
                    }

                    loadedIds.UnionWith(missingIds);

                    var dependencies = await query
                        .Where(f => missingIds.Contains(f.Id))
                        .ToListAsync(ct);

                    features.AddRange(dependencies);
                }

                var segmentIds = features
                    .SelectMany(f => f.Environments)
                    .SelectMany(e => e.Strategies)
                    .SelectMany(s => s.SegmentIds)
                    .Distinct()
                    .ToArray();

                var segments = await db.Segments
                    .AsNoTracking()
                    .Include(s => s.Constraints)
                    .Where(s => segmentIds.Contains(s.Id) && (s.ProjectId == null || s.ProjectId == projectId))
                    .ToListAsync(ct);

                var flags = features
                    .Select(f =>
                {
                    var e = f.Environments.SingleOrDefault();

                    return new FlagDefinition(f.Id, f.Name, e?.Enabled ?? false, f.ArchivedAt.HasValue, Math.Max(f.Revision, e?.Revision ?? 0), e?.Strategies
                        .Select(s => new StrategyDefinition(s.Id, s.Type, s.Enabled, s.SortOrder, s.Parameters, s.Constraints.ToArray(), s.SegmentIds.ToArray(), s.Variants.ToArray()))
                        .ToArray() ?? [], e?.Variants.ToArray() ?? [], f.Prerequisites.ToArray());
                })
                    .ToArray();
                var snapshot = new ConfigurationSnapshot(projectId, environmentId, environment.Enabled, DateTimeOffset.UtcNow, flags, segments.ToDictionary(s => s.Id, s => (IReadOnlyList<Domain.ValueObjects.Constraint>)s.Constraints.ToArray()));

                await transaction.CommitAsync(ct);

                foreach (var expired in keys.Keys
                    .Where(k => !k.StartsWith($"flags:{version}:", StringComparison.Ordinal))
                    .ToArray())
                {
                    cache.RemoveCache(expired);
                    keys.Remove(expired);
                }

                if (keys.Count >= 1000 && !keys.ContainsKey(key))
                {
                    var oldest = keys.MinBy(p => p.Value).Key;

                    cache.RemoveCache(oldest);
                    keys.Remove(oldest);
                }

                keys[key] = DateTime.UtcNow.Ticks;
                cache.Add(key, snapshot, DateTime.UtcNow.AddSeconds(Math.Max(options.Value.RefreshSeconds, options.Value.MaxStaleSeconds) + 1), null);

                return snapshot;
            }
            catch (Exception exception) when (!fresh && previous is not null && exception is not (OperationCanceledException or ArgumentException or KeyNotFoundException) && DateTimeOffset.UtcNow - previous.LoadedAt <= TimeSpan.FromSeconds(options.Value.MaxStaleSeconds))
            {
                logger.LogWarning(exception, "استفاده موقت از آخرین پیکربندی معتبر");

                return previous;
            }
        }
        finally
        {
            gate.Release();
        }
    }
}