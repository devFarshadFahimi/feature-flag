using System.Text.Json;
using Application.Features.ChangeRequests;
using Application.Features.Evaluations;
using Application.Features.Metrics;
using Domain.Aggregates.UsageAggregate;
using Domain.Enums;
using Infrastructure.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public sealed class ScheduledChangeExecutor(IServiceScopeFactory scopes, IConfigurationSnapshots snapshots, ILogger<ScheduledChangeExecutor> logger, IOptions<BackgroundWorkSettings> options) : BackgroundService
{
    /// <summary>Bounds each work category and isolates scheduling, aggregation, and retention failures.</summary>
    /// <param name = "stoppingToken">Cancels work during host shutdown.</param>
    /// <returns>A task representing the bounded background work loop.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Clamp(options.Value.TickSeconds, 1, 60)));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Bound work per tick; never hold one transaction for an unbounded queue.
                await RunPhaseAsync(
                    async ct =>
                {
                    for (var i = 0; i < Math.Clamp(options.Value.ScheduledPerTick, 1, 200); i++)
                    {
                        if (!await ExecuteOneAsync(ct))
                        {
                            break;
                        }
                    }
                }, "اجرای تغییرات زمان‌بندی‌شده ناموفق بود.", stoppingToken);
                await RunPhaseAsync(AggregateUsageAsync, "تجمیع آمار ارزیابی ناموفق بود.", stoppingToken);
                await RunPhaseAsync(FlushRateLimitHistoryAsync, "ذخیره تاریخچه محدودسازی درخواست‌ها ناموفق بود.", stoppingToken);
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "اجرای تغییرات زمان‌بندی‌شده ناموفق بود.");
            }
        }
    }

    private static bool IsInvalidBatch(Exception exception)
    {
        if (exception is OperationCanceledException || exception is Npgsql.NpgsqlException { IsTransient: true })
        {
            return false;
        }

        if (exception is JsonException or ArgumentException or OverflowException)
        {
            return true;
        }

        if (exception is Npgsql.PostgresException postgres)
        {
            return postgres.SqlState.StartsWith("22", StringComparison.Ordinal) || postgres.SqlState.StartsWith("23", StringComparison.Ordinal);
        }

        return exception is DbUpdateException { InnerException: { } inner } && IsInvalidBatch(inner);
    }

    // Reuses the existing worker; request handling never writes history directly to PostgreSQL.
    private async Task FlushRateLimitHistoryAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var settings = scope.ServiceProvider.GetService<RateLimitSettings>();

        if (settings?.Enabled != true)
        {
            return;
        }

        var history = scope.ServiceProvider.GetRequiredService<RateLimitHistorySettings>();
        var host = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var redis = scope.ServiceProvider.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var key = RateLimitHistory.StreamKey(settings.KeyPrefix, host.ApplicationName, host.EnvironmentName);

        await RateLimitHistory.FlushAsync(db, redis.GetDatabase(), key, history, logger, ct);
    }

    /// <summary>Isolates failures so unrelated work categories continue during the same tick.</summary>
    private async Task RunPhaseAsync(Func<CancellationToken, Task> action, string failure, CancellationToken ct)
    {
        try
        {
            await action(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Failure}", failure);
        }
    }

    /// <summary>Claims one due request and commits its changes atomically; failed attempts recover in a fresh transaction.</summary>
    /// <param name = "ct">Cancels the operation and its database or network requests.</param>
    /// <returns>Whether a due request was claimed for processing.</returns>
    private async Task<bool> ExecuteOneAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(8610044)", ct);

        var request = await db.ChangeRequests
            .FromSqlRaw("""
            SELECT * FROM "App"."TblChangeRequest"
            WHERE "Status" = 'Scheduled' AND "ScheduledAt" <= now()
            AND ("NextAttemptAt" IS NULL OR "NextAttemptAt" <= now())
            AND ("PredecessorId" IS NULL OR EXISTS
                (SELECT 1 FROM "App"."TblChangeRequest" predecessor WHERE predecessor."Id" = "TblChangeRequest"."PredecessorId"
                 AND predecessor."Status" <> 'Scheduled'))
            ORDER BY "ScheduledAt", "Id" LIMIT 1 FOR UPDATE SKIP LOCKED
            """)
            .Include(r => r.Items)
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);

        if (request is null)
        {
            return false;
        }

        try
        {
            await ConfigurationChanges.ApplyAsync(db, request, request.ScheduledBy ?? request.CreatedBy, ct);

            await transaction.CommitAsync(ct);

            snapshots.InvalidateAll();

            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await transaction.RollbackAsync(ct);

            var requestId = request.Id;

            db.ChangeTracker.Clear();

            await using var recovery = await db.Database.BeginTransactionAsync(ct);

            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(8610044)", ct);
            request = await db.ChangeRequests.SingleAsync(r => r.Id == requestId, ct);

            if (request.Status != ChangeRequestStatus.Scheduled)
            {
                return true;
            }

            if (exception is Npgsql.NpgsqlException { IsTransient: true } || exception.InnerException is Npgsql.NpgsqlException { IsTransient: true })
            {
                request.Retry("اختلال موقت پایگاه داده؛ اجرای مجدد زمان‌بندی شد.");
            }
            else
            {
                request.Suspend("اجرای مرحله به دلیل تغییر مجوز، بازبینی یا پیکربندی متوقف شد.");
            }

            if (request.Status != ChangeRequestStatus.Scheduled)
            {
                await ConfigurationChanges.SuspendSuccessorsAsync(db, request.Id, ct);
            }

            await db.SaveChangeAsync(ct);
            await recovery.CommitAsync(ct);

            logger.LogWarning(exception, "اجرای درخواست تغییر {RequestId} متوقف یا زمان‌بندی مجدد شد.", requestId);

            return true;
        }
    }

    /// <summary>Locks one batch at a time; aggregate writes and acknowledgement commit together across workers.</summary>
    private async Task AggregateUsageAsync(CancellationToken ct)
    {
        for (var i = 0; i < Math.Clamp(options.Value.UsageBatchesPerTick, 1, 1000); i++)
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var batch = await db.EvaluationBatches
                .FromSqlInterpolated($"""
                    SELECT * FROM "App"."EvaluationBatch"
                    WHERE "Status" = {EvaluationBatch.PendingStatus}
                    ORDER BY "RecordedAt", "Id" LIMIT 1 FOR UPDATE SKIP LOCKED
                    """)
                .SingleOrDefaultAsync(ct);

            if (batch is null)
            {
                return;
            }

            // A savepoint retains the claim while rolling back every partial aggregate write.
            await transaction.CreateSavepointAsync("usage_batch", ct);

            try
            {
                if (batch.Counts is null || batch.Counts.Length > 1_048_576)
                {
                    throw new ArgumentException("حجم داده دسته آمار معتبر نیست.");
                }

                var counts = JsonSerializer.Deserialize<ImpressionInput[]>(batch.Counts);

                if (batch.ProjectId == Guid.Empty || batch.EnvironmentId == Guid.Empty || string.IsNullOrWhiteSpace(batch.AppName) || batch.AppName.Length > 128 || batch.RecordedAt.Kind != DateTimeKind.Utc || batch.RecordedAt < DateTime.UnixEpoch || batch.RecordedAt > DateTime.UtcNow.AddMinutes(5) || counts is null || counts.Length is < 1 or > 1000 || counts.Any(count => count is null || string.IsNullOrWhiteSpace(count.FeatureName) || count.FeatureName.Length > 255 || string.IsNullOrWhiteSpace(count.Variant) || count.Variant.Length > 128 || count.Count is < 1 or > 1_000_000_000))
                {
                    throw new ArgumentException("اطلاعات دسته آمار معتبر نیست.");
                }

                if (!await db.Projects.AnyAsync(project => project.Id == batch.ProjectId, ct) || !await db.Environments.AnyAsync(environment => environment.Id == batch.EnvironmentId, ct))
                {
                    throw new ArgumentException("پروژه یا محیط دسته آمار یافت نشد.");
                }

                var grouped = counts
                    .GroupBy(count => new { count.FeatureName, count.Enabled, count.Variant })
                    .Select(items => new ImpressionInput(items.Key.FeatureName, items.Key.Enabled, items.Key.Variant, items.Sum(item => item.Count)))
                    .ToArray();
                var names = grouped
                    .Select(count => count.FeatureName)
                    .ToArray();

                await FlagMetrics.RecordAsync(db, batch.ProjectId, batch.EnvironmentId, batch.AppName, grouped, false, ct, batch.RecordedAt);
                await db.FeatureEnvironments
                    .Where(environment => environment.EnvironmentId == batch.EnvironmentId && environment.Feature.ProjectId == batch.ProjectId && names.Contains(environment.Feature.Name) && (environment.LastSeenAt == null || environment.LastSeenAt < batch.RecordedAt))
                    .ExecuteUpdateAsync(update => update.SetProperty(environment => environment.LastSeenAt, batch.RecordedAt), ct);
                db.EvaluationBatches.Remove(batch);

                await db.SaveChangeAsync(ct);
            }
            catch (Exception exception) when (IsInvalidBatch(exception))
            {
                await transaction.RollbackToSavepointAsync("usage_batch", ct);
                db.Entry(batch).State = EntityState.Unchanged;
                batch.Status = EvaluationBatch.QuarantinedStatus;
                batch.QuarantinedAt = DateTime.UtcNow;
                batch.FailureReason = "دسته آمار به دلیل داده نامعتبر، سرریز شمارنده یا ناسازگاری ذخیره‌سازی قرنطینه شد.";

                await db.SaveChangeAsync(ct);

                logger.LogWarning(exception, "دسته آمار {BatchId} برای بررسی قرنطینه شد.", batch.Id);
            }

            await transaction.CommitAsync(ct);
        }
    }

    /// <summary>Deletes bounded expired data; token evidence survives until expiry plus the retention window.</summary>
    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var settings = options.Value;
        var limit = Math.Clamp(settings.CleanupRowsPerTick, 1, 5000);
        var deduplicationCutoff = DateTime.UtcNow.AddDays(-Math.Clamp(settings.DeduplicationRetentionDays, 1, 3650));
        var aggregateCutoff = DateTime.UtcNow.AddDays(-Math.Clamp(settings.UsageRetentionDays, 1, 3650));
        var quarantineCutoff = DateTime.UtcNow.AddDays(-Math.Clamp(settings.QuarantineRetentionDays, 1, 3650));
        var tokenCutoff = DateTime.UtcNow.AddDays(-Math.Clamp(settings.TokenRetentionDays, 1, 3650));

        await RunPhaseAsync(
            async token =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                    DELETE FROM "App"."ImpressionBatch" WHERE ctid IN
                    (SELECT ctid FROM "App"."ImpressionBatch" WHERE "ReceivedAt" < {deduplicationCutoff} LIMIT {limit})
                    """, token);
        }, "پاک‌سازی سوابق تکرار آمار ناموفق بود.", ct);
        await RunPhaseAsync(
            async token =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                    DELETE FROM "App"."FlagUsage" WHERE ctid IN
                    (SELECT ctid FROM "App"."FlagUsage" WHERE "Hour" < {aggregateCutoff} LIMIT {limit})
                    """, token);
        }, "پاک‌سازی آمار قدیمی ناموفق بود.", ct);
        await RunPhaseAsync(
            async token =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                    DELETE FROM "App"."EvaluationBatch" WHERE ctid IN
                    (SELECT ctid FROM "App"."EvaluationBatch" WHERE "Status" = {EvaluationBatch.QuarantinedStatus} AND "QuarantinedAt" < {quarantineCutoff} LIMIT {limit})
                    """, token);
        }, "پاک‌سازی دسته‌های قرنطینه‌شده ناموفق بود.", ct);
        await RunPhaseAsync(
            async token =>
        {
            await db
                .Set<Domain.Aggregates.UserAggregate.RefreshToken>()
                .Where(refresh => refresh.ExpiresAt < tokenCutoff)
                .OrderBy(refresh => refresh.ExpiresAt)
                .Take(limit)
                .ExecuteDeleteAsync(token);
        }, "پاک‌سازی نشست‌های منقضی‌شده ناموفق بود.", ct);
        var history = scope.ServiceProvider.GetService<RateLimitHistorySettings>();

        if (history is not null)
        {
            var historyCutoff = DateTime.UtcNow.AddDays(-history.RetentionDays);

            await RunPhaseAsync(
                async token =>
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    DELETE FROM "App"."RateLimitRejectionSummary" WHERE ctid IN
                    (SELECT ctid FROM "App"."RateLimitRejectionSummary" WHERE "Minute" < {historyCutoff} LIMIT {limit})
                    """, token);
            }, "پاک‌سازی تاریخچه محدودسازی درخواست‌ها ناموفق بود.", ct);
        }
    }
}