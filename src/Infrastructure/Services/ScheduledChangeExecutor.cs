using Application.Features.ChangeRequests;
using Application.Features.Evaluations;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public sealed class ScheduledChangeExecutor(IServiceScopeFactory scopes, IConfigurationSnapshots snapshots, ILogger<ScheduledChangeExecutor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Bound work per tick; never hold one transaction for an unbounded queue.
                for (var i = 0; i < 20 && !stoppingToken.IsCancellationRequested; i++)
                {
                    if (!await ExecuteOneAsync(stoppingToken))
                    {
                        break;
                    }
                }

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

    /// <summary>Claims one due request and commits its changes atomically; failed attempts recover in a fresh transaction.</summary>
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

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deduplicationCutoff = DateTime.UtcNow.AddDays(-7);
        var aggregateCutoff = DateTime.UtcNow.AddDays(-90);
        // PostgreSQL tuple identity bounds each delete without loading metrics into process memory.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM "App"."ImpressionBatch" WHERE ctid IN
            (SELECT ctid FROM "App"."ImpressionBatch" WHERE "ReceivedAt" < {deduplicationCutoff} LIMIT 500)
            """, ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM "App"."FlagUsage" WHERE ctid IN
            (SELECT ctid FROM "App"."FlagUsage" WHERE "Hour" < {aggregateCutoff} LIMIT 500)
            """, ct);
    }
}