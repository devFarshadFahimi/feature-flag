using Application.Features.FeatureEnvironments;
using Application.Features.Segments.Commands.CreateSegment;
using Domain.ValueObjects;
using System.Text.Json;

namespace Application.Features.ChangeRequests;

public record PreviewChangeRequestQuery(Guid Id) : IQueryRequest<ChangeRequestPreview>;
public record ChangeRequestPreview(Guid Id, int RequiredApprovals, IReadOnlyList<ChangePreviewItem> Items);
public record ChangePreviewItem(string Action, Guid? FeatureId, JsonElement Before, JsonElement After);
public record EnvironmentConfigurationState(bool Enabled, List<ConfigureStrategyCommand> Strategies, List<VariantInput> Variants);
public record FeatureConfigurationState(string? Description, FeatureType Type, FeatureLifecycle Lifecycle, bool IsStale, bool ImpressionDataEnabled, List<string> Tags, List<Domain.Evaluation.Prerequisite> Prerequisites);
internal sealed class PreviewChangeRequestHandler(IApplicationDbContext db) : QueryRequestHandler<PreviewChangeRequestQuery, ChangeRequestPreview>
{
    /// <summary>Uses the real mutation path in memory; no changes, history or metrics are saved.</summary>
    public override async Task<ChangeRequestPreview> Handle(PreviewChangeRequestQuery request, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var change = await db.ChangeRequests
            .Include(r => r.Items)
            .SingleOrDefaultAsync(r => r.Id == request.Id, ct) ?? throw new KeyNotFoundException("درخواست تغییر یافت نشد.");

        _ = await ConfigurationChanges.InspectAsync(db, change, ct);

        var required = Math.Max(change.RequiredApprovals, Math.Max(1, await ConfigurationChanges.RequiredForRequestAsync(db, change, ct)));
        var items = new List<ChangePreviewItem>();

        foreach (var item in change.Items)
        {
            var before = await ConfigurationChanges.ReadStateAsync(db, change, item, ct);

            await ConfigurationChanges.ApplyPreviewItemAsync(db, change, item, ct);

            var after = await ConfigurationChanges.ReadStateAsync(db, change, item, ct);

            items.Add(new(item.Action, item.FeatureId, before, after));
        }

        return new(change.Id, required, items);
    }
}

public static partial class ConfigurationChanges
{
    public static EnvironmentConfigurationState Capture(FeatureEnvironment environment) => new(environment.Enabled, environment.Strategies
        .OrderBy(s => s.SortOrder)
        .ThenBy(s => s.Id)
        .Select(s => Capture(s, environment.Revision))
        .ToList(), environment.Variants
        .Select(Capture)
        .ToList());

    public static ConfigureStrategyCommand Capture(FeatureStrategy strategy, long revision) => new(strategy.FeatureId, strategy.EnvironmentId, strategy.Id, revision, strategy.Type, strategy.SortOrder, strategy.Enabled, strategy.Parameters.RolloutPercentage, strategy.Parameters.Stickiness, strategy.Parameters.GroupId, strategy.Parameters.UserIds.ToList(), strategy.Parameters.IpAddresses.ToList(), strategy.Parameters.ApplicationNames.ToList(), strategy.Constraints
        .Select(Capture)
        .ToList(), strategy.SegmentIds.ToList(), strategy.Variants
        .Select(Capture)
        .ToList());

    public static VariantInput Capture(Variant variant) => new(variant.Name, variant.Weight, variant.Payload, variant.Stickiness);

    public static ConstraintDto Capture(Constraint constraint) => new(constraint.ContextName, constraint.Operator, constraint.Values.ToList(), constraint.Inverted, constraint.CaseInsensitive);

    public static FeatureConfigurationState Capture(Feature feature) => new(feature.Description, feature.Type, feature.LifeCycle, feature.IsStale, feature.ImpressionDataEnabled, feature.Tags.ToList(), feature.Prerequisites.ToList());

    internal static Task ApplyPreviewItemAsync(IApplicationDbContext db, ChangeRequest request, ChangeRequestItem item, CancellationToken ct) => ApplyItemAsync(db, request, item, ct);

    internal static async Task<JsonElement> ReadStateAsync(IApplicationDbContext db, ChangeRequest request, ChangeRequestItem item, CancellationToken ct)
    {
        var payload = Parse(item);
        object state;

        if (item.Action == ChangeActionNames.ReplaceSegmentConstraints)
        {
            var segment = await db.Segments
                .Include(s => s.Constraints)
                .SingleAsync(s => s.Id == payload.SegmentId, ct);

            state = new
            {
                segment.Id,
                segment.Name,
                segment.Description,
                Constraints = segment.Constraints
                .Select(Capture)
                .ToArray()
            };
        }
        else if (item.Action == ChangeActionNames.SetGlobalProtection)
        {
            var environment = await db.Environments.SingleAsync(e => e.Id == request.EnvironmentId, ct);

            state = new
            {
                environment.Protected
            };
        }
        else if (item.Action == ChangeActionNames.SetProjectProtection)
        {
            var policy = db.ProjectEnvironmentPolicies.Local.SingleOrDefault(p => p.ProjectId == request.ProjectId && p.EnvironmentId == request.EnvironmentId) ?? await db.ProjectEnvironmentPolicies.SingleOrDefaultAsync(p => p.ProjectId == request.ProjectId && p.EnvironmentId == request.EnvironmentId, ct);

            state = new
            {
                Enabled = policy?.Enabled ?? false,
                RequiredApprovals = policy?.RequiredApprovals ?? 1
            };
        }
        else
        {
            var environment = await FeatureConfiguration.LoadAsync(db, item.FeatureId!.Value, request.EnvironmentId, ct);

            state = item.Action is ChangeActionNames.UpdateFeature or ChangeActionNames.SetPrerequisites or ChangeActionNames.RestoreFeature ? Capture(environment.Feature) : Capture(environment);
        }

        return JsonSerializer.SerializeToElement(state, Json);
    }
}