using Application.Features.FeatureEnvironments;
using Domain.Evaluation;
using Domain.ValueObjects;

namespace Application.Features.ChangeRequests;

public static partial class ConfigurationChanges
{
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
            await FeatureConfiguration.ConfigureAsync(db, environment, strategy with
            {
                FeatureId = environment.FeatureId,
                EnvironmentId = environment.EnvironmentId,
                StrategyId = null,
                ExpectedRevision = environment.Revision
            }, ct, validateOnly: true);
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

            await FeatureConfiguration.ConfigureAsync(db, environment, strategy with
            {
                FeatureId = environment.FeatureId,
                EnvironmentId = environment.EnvironmentId,
                StrategyId = null,
                SortOrder = index,
                ExpectedRevision = environment.Revision
            }, ct);
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
