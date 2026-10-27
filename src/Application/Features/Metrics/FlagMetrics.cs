using Domain.Evaluation;

namespace Application.Features.Metrics;

public record ImpressionInput(string FeatureName, bool Enabled, string Variant, long Count);
public record ReportImpressionsCommand(Guid ProjectId, Guid EnvironmentId, string AppName, Guid BatchId, List<ImpressionInput> Counts) : ICommandRequest;
public static class FlagMetrics
{
    public static async Task RecordAsync(IApplicationDbContext db, Guid projectId, Guid environmentId, string appName, IEnumerable<ImpressionInput> counts, bool impression, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var hour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

        foreach (var count in counts.Where(c => c.Count > 0))
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "App"."FlagUsage" ("ProjectId", "EnvironmentId", "AppName", "FeatureName", "Variant", "Enabled", "Impression", "Hour", "LastSeenAt", "Count")
                VALUES ({projectId}, {environmentId}, {appName}, {count.FeatureName}, {count.Variant}, {count.Enabled}, {impression}, {hour}, {now}, {count.Count})
                ON CONFLICT ("ProjectId", "EnvironmentId", "AppName", "FeatureName", "Variant", "Enabled", "Impression", "Hour")
                DO UPDATE SET "Count" = "FlagUsage"."Count" + EXCLUDED."Count", "LastSeenAt" = EXCLUDED."LastSeenAt"
                """, ct);
        }
    }

    public static async Task RecordEvaluationAsync(IApplicationDbContext db, Guid projectId, Guid environmentId, string appName, IReadOnlyList<FlagDecision> decisions, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await RecordAsync(db, projectId, environmentId, appName, decisions
            .Where(d => d.Reason != "not_found")
            .Select(d => new ImpressionInput(d.Name, d.Enabled, d.Variant.Name, 1)), false, ct);

        var names = decisions
            .Where(d => d.Reason != "not_found")
            .Select(d => d.Name)
            .ToArray();

        await db.FeatureEnvironments
            .Where(e => e.EnvironmentId == environmentId && e.Feature.ProjectId == projectId && names.Contains(e.Feature.Name))
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.LastSeenAt, DateTime.UtcNow), ct);

        await transaction.CommitAsync(ct);
    }
}

internal sealed class ReportImpressionsHandler(IApplicationDbContext db) : CommandRequestHandler<ReportImpressionsCommand>
{
    public override async Task<Result> Handle(ReportImpressionsCommand request, CancellationToken ct)
    {
        if (request.BatchId == Guid.Empty || string.IsNullOrWhiteSpace(request.AppName) || request.AppName.Length > 128 || request.Counts is null || request.Counts.Count is < 1 or > 100 || request.Counts.Any(c => string.IsNullOrWhiteSpace(c.FeatureName) || c.FeatureName.Length > 255 || string.IsNullOrWhiteSpace(c.Variant) || c.Variant.Length > 128 || c.Count is < 0 or > 1_000_000_000))
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

        if (await db.Features.CountAsync(f => f.ProjectId == request.ProjectId && names.Contains(f.Name), ct) != names.Length)
        {
            throw new ArgumentException("پرچم آمار باید متعلق به همین پروژه باشد.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var now = DateTime.UtcNow;
        var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "App"."ImpressionBatch" ("ProjectId", "EnvironmentId", "BatchId", "ReceivedAt")
            VALUES ({request.ProjectId}, {request.EnvironmentId}, {request.BatchId}, {now}) ON CONFLICT DO NOTHING
            """, ct);

        if (inserted > 0)
        {
            await FlagMetrics.RecordAsync(db, request.ProjectId, request.EnvironmentId, request.AppName, request.Counts, true, ct);
        }

        await transaction.CommitAsync(ct);

        return Ok();
    }
}