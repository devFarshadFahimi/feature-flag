using System.Text.Json;
using Domain.Aggregates.UsageAggregate;
using Domain.Evaluation;

namespace Application.Features.Metrics;

public record ImpressionInput(string FeatureName, bool Enabled, string Variant, long Count);
public record ReportImpressionsCommand(Guid ProjectId, Guid EnvironmentId, string AppName, Guid BatchId, List<ImpressionInput> Counts) : ICommandRequest;
public static class FlagMetrics
{
    public static async Task RecordAsync(IApplicationDbContext db, Guid projectId, Guid environmentId, string appName, IEnumerable<ImpressionInput> counts, bool impression, CancellationToken ct, DateTime? recordedAt = null)
    {
        var now = recordedAt ?? DateTime.UtcNow;
        var hour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

        foreach (var count in counts.Where(c => c.Count > 0))
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "App"."FlagUsage" ("ProjectId", "EnvironmentId", "AppName", "FeatureName", "Variant", "Enabled", "Impression", "Hour", "LastSeenAt", "Count")
                VALUES ({projectId}, {environmentId}, {appName}, {count.FeatureName}, {count.Variant}, {count.Enabled}, {impression}, {hour}, {now}, {count.Count})
                ON CONFLICT ("ProjectId", "EnvironmentId", "AppName", "FeatureName", "Variant", "Enabled", "Impression", "Hour")
                DO UPDATE SET "Count" = "FlagUsage"."Count" + EXCLUDED."Count", "LastSeenAt" = GREATEST("FlagUsage"."LastSeenAt", EXCLUDED."LastSeenAt")
                """, ct);
        }
    }

    /// <summary>Durably accepts evaluation usage before returning decisions; workers aggregate each batch atomically.</summary>
    /// <param name="db">The application database context participating in the current unit of work.</param>
    /// <param name="projectId">The identifier of the owning project.</param>
    /// <param name="environmentId">The identifier of the evaluation environment.</param>
    /// <param name="appName">The consuming application's name used to group usage.</param>
    /// <param name="decisions">The evaluated flag decisions to record as usage.</param>
    /// <param name="ct">Cancels the operation and its database or network requests.</param>
    /// <returns>A task that completes after evaluation usage has been durably accepted.</returns>
    public static async Task RecordEvaluationAsync(IApplicationDbContext db, Guid projectId, Guid environmentId, string appName, IReadOnlyList<FlagDecision> decisions, CancellationToken ct)
    {
        var counts = decisions
            .Where(decision => decision.Reason != "not_found")
            .Select(decision => new ImpressionInput(decision.Name, decision.Enabled, decision.Variant.Name, 1))
            .ToArray();

        if (counts.Length == 0)
        {
            return;
        }

        db.EvaluationBatches.Add(new EvaluationBatch { ProjectId = projectId, EnvironmentId = environmentId, AppName = appName, RecordedAt = DateTime.UtcNow, Counts = JsonSerializer.Serialize(counts) });

        try
        {
            await db.SaveChangeAsync(ct);
        }
        catch (DbUpdateException exception)
        {
            throw new InvalidOperationException("ثبت پایدار آمار ارزیابی موقتاً در دسترس نیست.", exception);
        }
    }
}

internal sealed class ReportImpressionsHandler(IApplicationDbContext db) : CommandRequestHandler<ReportImpressionsCommand>
{
    public override async Task<Result> Handle(ReportImpressionsCommand request, CancellationToken ct)
    {
        if (request.BatchId == Guid.Empty || string.IsNullOrWhiteSpace(request.AppName) || request.AppName.Length > 128 || request.Counts is null || request.Counts.Count is < 1 or > 100 || request.Counts.Any(c => c is null || string.IsNullOrWhiteSpace(c.FeatureName) || c.FeatureName.Length > 255 || string.IsNullOrWhiteSpace(c.Variant) || c.Variant.Length > 128 || c.Count is < 0 or > 1_000_000_000))
        {
            throw new ArgumentException("اطلاعات دسته آمار معتبر نیست.");
        }

        if (!await db.Environments.AnyAsync(e => e.Id == request.EnvironmentId, ct))
        {
            throw new KeyNotFoundException("محیط یافت نشد.");
        }

        var names = request.Counts
            .Select(c => c.FeatureName)
            .Distinct()
            .ToArray();

        var features = await db.Features
            .AsNoTracking()
            .Where(feature => feature.ProjectId == request.ProjectId && names.Contains(feature.Name))
            .Select(feature => new { feature.Name, feature.ImpressionDataEnabled })
            .ToListAsync(ct);

        if (features.Count != names.Length)
        {
            throw new ArgumentException("پرچم آمار باید متعلق به همین پروژه باشد.");
        }

        var enabledNames = features
            .Where(feature => feature.ImpressionDataEnabled)
            .Select(feature => feature.Name)
            .ToHashSet(StringComparer.Ordinal);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var now = DateTime.UtcNow;
        var inserted = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "App"."ImpressionBatch" ("ProjectId", "EnvironmentId", "BatchId", "ReceivedAt")
            VALUES ({request.ProjectId}, {request.EnvironmentId}, {request.BatchId}, {now}) ON CONFLICT DO NOTHING
            """, ct);

        if (inserted > 0)
        {
            await FlagMetrics.RecordAsync(db, request.ProjectId, request.EnvironmentId, request.AppName, request.Counts.Where(count => enabledNames.Contains(count.FeatureName)), true, ct);
        }

        await transaction.CommitAsync(ct);

        return Ok();
    }
}
