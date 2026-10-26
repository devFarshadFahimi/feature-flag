using BusinessMakerFramework.Shared.Abstractions.UserManagement;
using Domain.Evaluation;

namespace Application.Features.Evaluations;

public interface IConfigurationSnapshots
{
    Task<ConfigurationSnapshot> LoadAsync(Guid projectId, Guid environmentId, bool fresh, CancellationToken cancellationToken);
    void InvalidateAll();
}

public record EvaluateFlagsCommand(Guid ProjectId, Guid EnvironmentId, string AppName, EvaluationContext Context, List<string>? FeatureNames = null, Dictionary<string, bool>? Defaults = null, bool Preview = false) : IQueryRequest<EvaluationResponse>;
public record EvaluationResponse(IReadOnlyList<FlagDecision> Features, DateTimeOffset EvaluatedAt, DateTimeOffset SnapshotLoadedAt, bool Stale, int RefreshAfterSeconds, int MaxStaleSeconds);
internal sealed class EvaluateFlagsHandler(IApplicationDbContext db, IConfigurationSnapshots snapshots, IUserInfoService actor, IOptions<EvaluationSettings> options, Microsoft.Extensions.Logging.ILogger<EvaluateFlagsHandler> logger) : QueryRequestHandler<EvaluateFlagsCommand, EvaluationResponse>
{
    public override async Task<EvaluationResponse> Handle(EvaluateFlagsCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.AppName) || request.AppName.Length > 128 || request.Context is null || request.FeatureNames?.Count > 100 || request.FeatureNames?.Any(n => string.IsNullOrEmpty(n) || n.Length > 255) == true || request.Defaults?.Count > 100 || request.Context.Properties?.Count > 50 || request.Context.Properties?.Any(p => p.Key.Length > 128 || p.Value is null || p.Value.Length > 1024) == true || new[]
        {
            request.Context.UserId,
            request.Context.SessionId,
            request.Context.RemoteAddress,
            request.Context.ApplicationHostname
        }.Any(v => v?.Length > 1024))
        {
            throw new ArgumentException("نام برنامه، زمینه یا فهرست پرچم‌ها معتبر نیست.");
        }

        if (!await db
            .VisibleProjects(actor)
            .AnyAsync(p => p.Id == request.ProjectId, ct))
        {
            throw new System.Security.SecurityException("اجازه ارزیابی این پروژه را ندارید.");
        }


        var snapshot = await snapshots.LoadAsync(request.ProjectId, request.EnvironmentId, request.Preview, ct);
        var now = DateTimeOffset.UtcNow;
        var names = request.FeatureNames?.Distinct(StringComparer.Ordinal).ToArray() ?? snapshot.Flags
            .Where(f => !f.Archived)
            .Select(f => f.Name)
            .ToArray();
        var results = names
            .Select(name =>
        {
            var flag = snapshot.Flags.FirstOrDefault(f => f.Name == name);

            return flag is null ? new FlagDecision(name, request.Defaults?.GetValueOrDefault(name) ?? false, FlagEvaluator.DisabledVariant, "not_found", 0) : FlagEvaluator.Evaluate(snapshot, flag, request.Context, now, request.Preview);
        })
            .ToArray();

        if (!request.Preview)
        {
            try
            {
                await Metrics.FlagMetrics.RecordEvaluationAsync(db, request.ProjectId, request.EnvironmentId, request.AppName, results, ct);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogError(logger, exception, "ثبت آمار ارزیابی ناموفق بود.");
            }
        }


        return new(results, now, snapshot.LoadedAt, now - snapshot.LoadedAt >= TimeSpan.FromSeconds(options.Value.RefreshSeconds), options.Value.RefreshSeconds, options.Value.MaxStaleSeconds);
    }
}

public sealed class EvaluationSettings
{
    public int RefreshSeconds { get; set; } = 5;
    public int MaxStaleSeconds { get; set; } = 60;
}