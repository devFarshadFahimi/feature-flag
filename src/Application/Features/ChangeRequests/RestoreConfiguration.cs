using Application.Features.FeatureEnvironments;
using Application.Features.Segments.Commands.CreateSegment;
using Domain.Aggregates.SegmentAggregate;
using Domain.Evaluation;
using Domain.ValueObjects;

namespace Application.Features.ChangeRequests;

/// <summary>Validates governed configuration changes and applies them through aggregate transitions.</summary>
public static partial class ConfigurationChanges
{
    private static void ValidateSegmentState(SegmentConfigurationState state)
    {
        if (string.IsNullOrWhiteSpace(state.Name) || state.Name.Length > 128 || state.Description?.Length > 2000 || state.Constraints is null || state.Constraints.Count > 100 || state.Constraints.Any(constraint => constraint is null))
        {
            throw new ArgumentException("تصویر پیکربندی بخش معتبر نیست.");
        }

        foreach (var constraint in state.Constraints)
        {
            _ = Constraint.Create(constraint.ContextName, constraint.Operator, constraint.Values, constraint.Inverted, constraint.CaseInsensitive);
        }
    }

    private static void ValidateProjectState(ProjectConfigurationState state)
    {
        if (string.IsNullOrWhiteSpace(state.Name) || state.Name.Length > 255 || state.Description is null || state.Description.Length > 2000 || string.IsNullOrWhiteSpace(state.DefaultStickiness) || state.DefaultStickiness.Length > 64 || state.FeatureLimit is < 1)
        {
            throw new ArgumentException("تصویر پیکربندی پروژه معتبر نیست.");
        }
    }

    private static void ValidateGlobalEnvironmentState(GlobalEnvironmentState state)
    {
        if (string.IsNullOrWhiteSpace(state.Name) || state.Name.Length > 128)
        {
            throw new ArgumentException("تصویر پیکربندی محیط معتبر نیست.");
        }
    }

    /// <summary>Restores segment metadata and constraints, including deleted segments, without resetting revision history.</summary>
    private static async Task RestoreSegmentAsync(IApplicationDbContext db, ChangePayload payload, CancellationToken ct)
    {
        var state = payload.SegmentState!;
        var id = payload.SegmentId!.Value;
        var segment = db.Segments.Local.SingleOrDefault(value => value.Id == id) ?? await db.Segments
            .Include(value => value.Constraints)
            .SingleOrDefaultAsync(value => value.Id == id, ct);

        if (segment is null)
        {
            var revision = await db.ConfigurationHistory
                .Where(history => history.ResourceType == nameof(Segment) && history.ResourceId == id.ToString())
                .MaxAsync(history => history.Revision, ct);

            segment = Segment.RestoreDeleted(id, state.ProjectId, state.Name, state.Description, state.IsPublic, revision);
            db.Segments.Add(segment);
        }
        else
        {
            db.SegmentConstraints.RemoveRange(segment.Constraints);
        }

        segment.RestoreConfiguration(state.Name, state.Description, state.IsPublic, state.Constraints.Select(constraint => Constraint.Create(constraint.ContextName, constraint.Operator, constraint.Values, constraint.Inverted, constraint.CaseInsensitive)));
    }

    /// <summary>Rejects incomplete or foreign history before it can enter the approval workflow.</summary>
    private static async Task ValidateRestoreAsync(IApplicationDbContext db, FeatureEnvironment environment, string action, ChangePayload payload, CancellationToken ct)
    {
        if (action == ChangeActionNames.RestoreFeature)
        {
            var state = payload.FeatureState ?? throw new ArgumentException("تصویر پیکربندی پرچم الزامی است.");

            if (!Enum.IsDefined(state.Type) || !Enum.IsDefined(state.Lifecycle) || state.Description?.Length > 2000 || state.Tags is null || state.Prerequisites is null || state.Tags.Count > 100 || state.Tags.Any(t => string.IsNullOrWhiteSpace(t) || t.Length > 255) || state.Prerequisites.Count > 100)
            {
                throw new ArgumentException("تصویر پیکربندی پرچم معتبر نیست.");
            }

            var flags = await db.Features
                .AsNoTracking()
                .Where(f => f.ProjectId == environment.Feature.ProjectId)
                .ToListAsync(ct);

            var graph = flags.ToDictionary(f => f.Id, f => (IReadOnlyList<Prerequisite>)f.Prerequisites);

            graph[environment.FeatureId] = state.Prerequisites;
            FlagEvaluator.ValidatePrerequisites(graph);

            return;
        }

        var configuration = payload.EnvironmentState ?? throw new ArgumentException("تصویر پیکربندی محیط الزامی است.");

        if (configuration.Strategies is null || configuration.Variants is null || configuration.Strategies.Count > 100 || configuration.Variants.Count > 100)
        {
            throw new ArgumentException("تصویر پیکربندی محیط معتبر نیست.");
        }

        Variant.ValidateSet(configuration.Variants
            .Select(v => Variant.Create(v.Name, v.Weight, v.Payload, v.Stickiness))
            .ToArray());
        foreach (var strategy in configuration.Strategies)
        {
            var strategyInput = strategy with
            {
                FeatureId = environment.FeatureId,
                EnvironmentId = environment.EnvironmentId,
                StrategyId = null,
                ExpectedRevision = environment.Revision
            };

            await FeatureConfiguration.ConfigureAsync(db, environment, strategyInput, ct, validateOnly: true);
        }
    }

    /// <summary>Rebuilds the complete environment configuration; new strategy identities prevent reuse of deleted children.</summary>
    private static async Task RestoreEnvironmentAsync(IApplicationDbContext db, FeatureEnvironment environment, EnvironmentConfigurationState state, CancellationToken ct)
    {
        foreach (var strategy in environment.Strategies.ToArray())
        {
            environment.RemoveStrategy(strategy.Id);
        }

        var ordered = state.Strategies
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.StrategyId)
            .ToArray();

        for (var index = 0; index < ordered.Length; index++)
        {
            var strategy = ordered[index];

            var strategyInput = strategy with
            {
                FeatureId = environment.FeatureId,
                EnvironmentId = environment.EnvironmentId,
                StrategyId = null,
                SortOrder = index,
                ExpectedRevision = environment.Revision
            };

            await FeatureConfiguration.ConfigureAsync(db, environment, strategyInput, ct);
        }

        db.FeatureVariants.RemoveRange(environment.Variants);
        environment.SetVariants(state.Variants.Select(v => Variant.Create(v.Name, v.Weight, v.Payload, v.Stickiness)));

        if (state.Enabled)
        {
            environment.Enable();
        }
        else
        {
            environment.Disable();
        }
    }
}

public sealed record SegmentConfigurationState(string Name, string? Description, bool IsPublic, Guid? ProjectId, List<ConstraintDto> Constraints);
public sealed record ProjectConfigurationState(string Name, string Description, string DefaultStickiness, int? FeatureLimit);
public sealed record GlobalEnvironmentState(string Name, int SortOrder, bool Enabled, bool Protected);
