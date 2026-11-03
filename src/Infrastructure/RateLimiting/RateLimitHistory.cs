using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.RateLimiting;

internal static class RateLimitHistory
{
    private const string PayloadField = "payload";
    private const int WorkerLockNamespace = 8610046;
    private const long CapacityLogIntervalMilliseconds = 60_000;
    private static long lastCapacityLogMilliseconds = -CapacityLogIntervalMilliseconds;

    internal static string StreamKey(string prefix, string application, string environment)
    {
        var scope = JsonSerializer.Serialize(new[] { application, environment });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scope)));

        return $"{prefix}:rejection-history:{hash}:pending";
    }

    // The stream is a bounded, temporary transfer buffer, not a table of individual requests.
    internal static async Task EnqueueAsync(IDatabase redis, string prefix, RejectedRequest observation, RateLimitHistorySettings settings, ILogger logger, CancellationToken ct)
    {
        var key = StreamKey(prefix, observation.Application, observation.Environment);

        var pending = await redis
            .StreamLengthAsync(key)
            .WaitAsync(ct);

        if (pending >= settings.MaxPendingEvents)
        {
            var now = System.Environment.TickCount64;
            var previous = Interlocked.Read(ref lastCapacityLogMilliseconds);

            if (now - previous >= CapacityLogIntervalMilliseconds && Interlocked.CompareExchange(ref lastCapacityLogMilliseconds, now, previous) == previous)
            {
                logger.LogWarning("ظرفیت صف تاریخچه محدودسازی تکمیل است؛ مشاهده‌های جدید ثبت نمی‌شوند.");
            }

            return;
        }

        var transaction = redis.CreateTransaction();
        var append = transaction.StreamAddAsync(key, [new NameValueEntry(PayloadField, JsonSerializer.Serialize(observation))], maxLength: settings.MaxPendingEvents, useApproximateMaxLength: false);
        var expire = transaction.KeyExpireAsync(key, TimeSpan.FromHours(settings.BufferRetentionHours));

        await transaction
            .ExecuteAsync()
            .WaitAsync(ct);
        await Task
            .WhenAll(append, expire)
            .WaitAsync(ct);
    }

    // One transaction owns the checkpoint and all minute-summary increments. Redis acknowledgement occurs only after commit.
    internal static async Task FlushAsync(ApplicationDbContext db, IDatabase redis, string key, RateLimitHistorySettings settings, ILogger logger, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var streamLock = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        var acquired = await db.Database
            .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({WorkerLockNamespace}, {streamLock}) AS \"Value\"")
            .SingleAsync(ct);

        if (!acquired)
        {
            return;
        }

        var checkpoint = await db
            .Set<RateLimitHistoryCheckpoint>()
            .SingleOrDefaultAsync(row => row.StreamKey == key, ct);

        // Recover acknowledgements lost after a previous SQL commit, including a process crash during shutdown.
        if (checkpoint is not null)
        {
            var acknowledged = await redis
                .StreamRangeAsync(key, maxId: checkpoint.LastStreamId, count: settings.BatchSize)
                .WaitAsync(ct);

            if (acknowledged.Length > 0)
            {
                var acknowledgedIds = acknowledged
                    .Select(entry => entry.Id)
                    .ToArray();

                await redis
                    .StreamDeleteAsync(key, acknowledgedIds)
                    .WaitAsync(ct);
            }
        }

        var entries = await redis
            .StreamReadAsync(key, checkpoint?.LastStreamId ?? "0-0", settings.BatchSize)
            .WaitAsync(ct);

        if (entries.Length == 0)
        {
            return;
        }

        var observations = new List<RejectedRequest>(entries.Length);
        var discarded = 0;

        foreach (var entry in entries)
        {
            try
            {
                var payload = entry[PayloadField].ToString();

                if (payload.Length > 8192)
                {
                    throw new JsonException("اندازه مشاهده صف تاریخچه محدودسازی معتبر نیست.");
                }

                var observation = JsonSerializer.Deserialize<RejectedRequest>(payload);

                if (!Valid(observation, settings))
                {
                    throw new JsonException("مشاهده صف تاریخچه محدودسازی معتبر نیست.");
                }

                observations.Add(observation!);
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException)
            {
                discarded++;
                logger.LogWarning("رکورد نامعتبر تاریخچه محدودسازی با شناسه {EntryId} کنار گذاشته شد.", entry.Id.ToString());
            }
        }

        var groups = observations.GroupBy(observation => new
        {
            observation.IpAddress,
            observation.Application,
            observation.Environment,
            observation.Method,
            observation.Route,
            Minute = new DateTime(observation.RecordedAt.Year, observation.RecordedAt.Month, observation.RecordedAt.Day, observation.RecordedAt.Hour, observation.RecordedAt.Minute, 0, DateTimeKind.Utc)
        });

        var summaries = groups
            .Select(group => new
            {
                BucketKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[]
                {
                    group.Key.Application, group.Key.Environment, group.Key.IpAddress, group.Key.Method, group.Key.Route
                })))),
                group.Key.Minute,
                group.Key.IpAddress,
                group.Key.Application,
                group.Key.Environment,
                group.Key.Method,
                group.Key.Route,
                RejectedRequests = group.LongCount(),
                FirstRejectedAt = group.Min(observation => observation.RecordedAt),
                LastRejectedAt = group.Max(observation => observation.RecordedAt)
            })
            .ToArray();

        if (summaries.Length > 0)
        {
            var payload = JsonSerializer.Serialize(summaries);

            // One parameterized statement per batch, including when every observation belongs to a different IP.
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "App"."RateLimitRejectionSummary"
                ("BucketKey", "Minute", "IpAddress", "Application", "Environment", "Method", "Route", "RejectedRequests", "FirstRejectedAt", "LastRejectedAt")
                SELECT summary."BucketKey", summary."Minute", summary."IpAddress", summary."Application", summary."Environment",
                    summary."Method", summary."Route", summary."RejectedRequests", summary."FirstRejectedAt", summary."LastRejectedAt"
                FROM jsonb_to_recordset(CAST({payload} AS jsonb)) AS summary
                    ("BucketKey" text, "Minute" timestamptz, "IpAddress" text, "Application" text, "Environment" text,
                     "Method" text, "Route" text, "RejectedRequests" bigint, "FirstRejectedAt" timestamptz, "LastRejectedAt" timestamptz)
                ON CONFLICT ("BucketKey", "Minute") DO UPDATE SET
                    "RejectedRequests" = "RateLimitRejectionSummary"."RejectedRequests" + EXCLUDED."RejectedRequests",
                    "FirstRejectedAt" = LEAST("RateLimitRejectionSummary"."FirstRejectedAt", EXCLUDED."FirstRejectedAt"),
                    "LastRejectedAt" = GREATEST("RateLimitRejectionSummary"."LastRejectedAt", EXCLUDED."LastRejectedAt")
                """, ct);
        }

        if (checkpoint is null)
        {
            checkpoint = new RateLimitHistoryCheckpoint { StreamKey = key };
            db.Add(checkpoint);
        }

        checkpoint.LastStreamId = entries[^1].Id.ToString();
        checkpoint.LastProcessedAt = DateTime.UtcNow;
        checkpoint.DiscardedEntries += discarded;

        await db.SaveChangeAsync(ct);
        await transaction.CommitAsync(ct);

        var committedIds = entries
            .Select(entry => entry.Id)
            .ToArray();

        await redis
            .StreamDeleteAsync(key, committedIds)
            .WaitAsync(ct);
    }

    private static bool Valid(RejectedRequest? observation, RateLimitHistorySettings settings)
    {
        return observation is not null
            && observation.Version == 1
            && observation.IpAddress is { Length: > 0 and <= 45 }
            && IPAddress.TryParse(observation.IpAddress, out _)
            && observation.Application is { Length: > 0 and <= 128 }
            && observation.Environment is { Length: > 0 and <= 64 }
            && observation.Method is { Length: > 0 and <= 16 }
            && observation.Route is { Length: > 0 and <= 1024 }
            && observation.RecordedAt.Kind == DateTimeKind.Utc
            && observation.RecordedAt >= DateTime.UtcNow.AddDays(-settings.RetentionDays)
            && observation.RecordedAt <= DateTime.UtcNow.AddMinutes(5);
    }

    internal sealed record RejectedRequest(string IpAddress, string Application, string Environment, string Method, string Route, DateTime RecordedAt, int Version = 1);
}

public sealed class RateLimitHistorySettings
{
    public const string SectionName = "RateLimitHistory";

    public int RetentionDays { get; set; } = 30;

    public int BufferRetentionHours { get; set; } = 24;

    public int MaxPendingEvents { get; set; } = 100_000;

    public int BatchSize { get; set; } = 1000;

    public bool IsValid() => RetentionDays is >= 1 and <= 365 && BufferRetentionHours is >= 1 and <= 168
        && MaxPendingEvents is >= 100 and <= 1_000_000 && BatchSize is >= 1 and <= 5000 && BatchSize <= MaxPendingEvents;
}
