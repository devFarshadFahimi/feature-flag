using Application.Features.Segments.Commands.CreateSegment;
using Domain.Evaluation;
using Domain.ValueObjects;

namespace Application.Features.FeatureEnvironments;

public record VariantInput(string Name, int Weight, Dictionary<string, object>? Payload = null, string? Stickiness = null);
public record ConfigureStrategyCommand(Guid FeatureId, Guid EnvironmentId, Guid? StrategyId, long ExpectedRevision, StrategyType Type, int SortOrder = 0, bool Enabled = true, int? RolloutPercentage = null, string? Stickiness = null, string? GroupId = null, List<string>? UserIds = null, List<string>? IpAddresses = null, List<string>? ApplicationNames = null, List<ConstraintDto>? Constraints = null, List<int>? SegmentIds = null, List<VariantInput>? Variants = null) : ICommandRequest<Guid>;
public record ReorderStrategiesCommand(Guid FeatureId, Guid EnvironmentId, long ExpectedRevision, List<Guid> StrategyIds) : ICommandRequest;
public record SetPrerequisitesCommand(Guid FeatureId, long ExpectedRevision, List<Prerequisite> Prerequisites) : ICommandRequest;
// Used by direct management and by validated change-request actions.
public static class FeatureConfiguration
{
    public static async Task<FeatureEnvironment> LoadAsync(IApplicationDbContext db, Guid featureId, Guid environmentId, CancellationToken ct) => await db.FeatureEnvironments
        .Include(e => e.Strategies)
        .ThenInclude(s => s.Constraints)
        .Include(e => e.Strategies)
        .ThenInclude(s => s.Variants)
        .Include(e => e.Variants)
        .Include(e => e.Feature)
        .SingleOrDefaultAsync(e => e.FeatureId == featureId && e.EnvironmentId == environmentId, ct) ?? throw new KeyNotFoundException("ارتباط پرچم و محیط یافت نشد.");
    public static void RequireRevision(long expected, long actual)
    {
        if (expected != actual)
        {
            throw new DbUpdateConcurrencyException("نسخه اطلاعات تغییر کرده است؛ دوباره بارگذاری کنید.");
        }
    }

    public static async Task<Guid> ConfigureAsync(IApplicationDbContext db, FeatureEnvironment environment, ConfigureStrategyCommand input, CancellationToken ct, bool validateOnly = false)
    {
        RequireRevision(input.ExpectedRevision, environment.Revision);

        if (input.FeatureId != environment.FeatureId || input.EnvironmentId != environment.EnvironmentId)
        {
            throw new ArgumentException("شناسه‌های پیکربندی یکسان نیستند.");
        }

        if (!Enum.IsDefined(input.Type) || input.Type == StrategyType.Custom || input.SortOrder < 0 || input.Stickiness?.Length > 128 || input.GroupId?.Length > 128)
        {
            throw new ArgumentException("نوع، ترتیب یا پارامترهای راهبرد معتبر نیست.");
        }


        var parameters = input.Type switch
        {
            StrategyType.Default => StrategyParameters.CreateDefault(),
            StrategyType.GradualRollout or StrategyType.FlexibleRollout => StrategyParameters.CreateGradualRollout(input.RolloutPercentage ?? throw new ArgumentException("درصد انتشار الزامی است."), input.Stickiness ?? "default", input.GroupId),
            StrategyType.UserWithId => StrategyParameters.CreateUserIds(input.UserIds ?? []),
            StrategyType.RemoteAddress => StrategyParameters.CreateRemoteAddress(input.IpAddresses ?? []),
            StrategyType.ApplicationHost => StrategyParameters.CreateApplicationHost(input.ApplicationNames ?? []),
            _ => throw new ArgumentException("نوع راهبرد پشتیبانی نمی‌شود.")
        };

        if ((input.UserIds?.Count ?? 0) > 1000 || (input.IpAddresses?.Count ?? 0) > 1000 || (input.ApplicationNames?.Count ?? 0) > 1000 || (input.Constraints?.Count ?? 0) > 100 || (input.SegmentIds?.Count ?? 0) > 100 || (input.Variants?.Count ?? 0) > 100)
        {
            throw new ArgumentException("تعداد پارامترهای راهبرد بیش از حد مجاز است.");
        }

        if (parameters.IpAddresses.Any(ip => !FlagEvaluator.IpMatches(ip.Split('/')[0], ip)))
        {
            throw new ArgumentException("نشانی شبکه یا محدوده آن معتبر نیست.");
        }

        if (parameters.UserIds
            .Concat(parameters.ApplicationNames)
            .Any(s => string.IsNullOrWhiteSpace(s) || s.Length > 1024))
        {
            throw new ArgumentException("مقدار پارامتر راهبرد معتبر نیست.");
        }


        var segments = (input.SegmentIds ?? [])
            .Distinct()
            .ToArray();

        if (await db.Segments.CountAsync(s => segments.Contains(s.Id) && (s.ProjectId == null || s.ProjectId == environment.Feature.ProjectId), ct) != segments.Length)
        {
            throw new ArgumentException("بخش انتخاب‌شده یافت نشد یا متعلق به پروژه دیگری است.");
        }


        var constraints = (input.Constraints ?? [])
            .Select(c => Constraint.Create(c.ContextName, c.Operator, c.Values, c.Inverted, c.CaseInsensitive))
            .ToList();
        var variants = (input.Variants ?? [])
            .Select(v => Variant.Create(v.Name, v.Weight, v.Payload, v.Stickiness))
            .ToList();

        Variant.ValidateSet(variants);

        if (input.StrategyId.HasValue)
        {
            var existing = environment.Strategies.SingleOrDefault(s => s.Id == input.StrategyId) ?? throw new KeyNotFoundException("راهبرد یافت نشد.");

            if (existing.Type != input.Type)
            {
                throw new ArgumentException("برای تغییر نوع، راهبرد جدید ایجاد کنید.");
            }
        }

        if (validateOnly)
        {
            return input.StrategyId ?? Guid.Empty;
        }


        FeatureStrategy strategy;

        if (input.StrategyId.HasValue)
        {
            strategy = environment.Strategies.SingleOrDefault(s => s.Id == input.StrategyId) ?? throw new KeyNotFoundException("راهبرد یافت نشد.");

            if (strategy.Type != input.Type)
            {
                throw new ArgumentException("برای تغییر نوع، راهبرد جدید ایجاد کنید.");
            }


            strategy.UpdateParameters(parameters);
            strategy.Constraints.Clear();
            strategy.Constraints.AddRange(constraints);
            strategy.SegmentIds.Clear();
            strategy.SegmentIds.AddRange(segments);
        }
        else
        {
            strategy = environment.AddStrategy(input.Type, parameters, constraints, segments);
            db.FeatureStrategies.Add(strategy);
        }


        strategy.UpdateSortOrder(input.SortOrder);
        strategy.SetEnabled(input.Enabled);
        strategy.SetVariants(variants);
        // Touch the aggregate even when only a child changed, so optimistic concurrency covers the whole configuration.
        environment.ConfigurationChanged();

        return strategy.Id;
    }
}

internal sealed class ConfigureStrategyHandler(IApplicationDbContext db) : CommandRequestHandler<ConfigureStrategyCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(ConfigureStrategyCommand request, CancellationToken ct)
    {
        var environment = await FeatureConfiguration.LoadAsync(db, request.FeatureId, request.EnvironmentId, ct);
        var id = await FeatureConfiguration.ConfigureAsync(db, environment, request, ct);

        await db.SaveChangeAsync(ct);

        return Ok(id);
    }
}

internal sealed class ReorderStrategiesHandler(IApplicationDbContext db) : CommandRequestHandler<ReorderStrategiesCommand>
{
    public override async Task<Result> Handle(ReorderStrategiesCommand request, CancellationToken ct)
    {
        var environment = await FeatureConfiguration.LoadAsync(db, request.FeatureId, request.EnvironmentId, ct);

        FeatureConfiguration.RequireRevision(request.ExpectedRevision, environment.Revision);

        if (request.StrategyIds.Count != environment.Strategies.Count || request.StrategyIds
            .Distinct()
            .Count() != request.StrategyIds.Count || environment.Strategies.Any(s => !request.StrategyIds.Contains(s.Id)))
        {
            throw new ArgumentException("فهرست ترتیب راهبردها کامل و یکتا نیست.");
        }


        for (var i = 0; i < request.StrategyIds.Count; i++)
        {
            environment.Strategies
                .Single(s => s.Id == request.StrategyIds[i])
                .UpdateSortOrder(i);
        }

        environment.ConfigurationChanged();

        await db.SaveChangeAsync(ct);

        return Ok();
    }
}

internal sealed class SetPrerequisitesHandler(IApplicationDbContext db) : CommandRequestHandler<SetPrerequisitesCommand>
{
    public override async Task<Result> Handle(SetPrerequisitesCommand request, CancellationToken ct)
    {
        var feature = await db.Features.SingleOrDefaultAsync(f => f.Id == request.FeatureId, ct) ?? throw new KeyNotFoundException("پرچم یافت نشد.");

        FeatureConfiguration.RequireRevision(request.ExpectedRevision, feature.Revision);

        var flags = await db.Features
            .Where(f => f.ProjectId == feature.ProjectId)
            .ToListAsync(ct);

        if (request.Prerequisites.Count > 20 || request.Prerequisites
            .Select(p => p.FeatureId)
            .Distinct()
            .Count() != request.Prerequisites.Count || request.Prerequisites.Any(p => !flags.Any(f => f.Id == p.FeatureId)))
        {
            throw new ArgumentException("پیش‌نیازهای پرچم معتبر نیستند.");
        }


        feature.SetPrerequisites(request.Prerequisites);
        FlagEvaluator.ValidatePrerequisites(flags.ToDictionary(f => f.Id, f => (IReadOnlyList<Prerequisite>)f.Prerequisites));

        await db.SaveChangeAsync(ct);

        return Ok();
    }
}