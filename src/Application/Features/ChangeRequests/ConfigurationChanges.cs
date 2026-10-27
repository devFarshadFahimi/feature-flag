using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Features.FeatureEnvironments;
using Application.Features.Segments.Commands.CreateSegment;
using Domain.Evaluation;
using Domain.ValueObjects;

namespace Application.Features.ChangeRequests;

public sealed record ChangePayload(int Version, long ExpectedRevision, bool? Enabled = null, ConfigureStrategyCommand? Strategy = null, Guid? StrategyId = null, List<Guid>? StrategyIds = null, List<VariantInput>? Variants = null, int? SegmentId = null, List<ConstraintDto>? Constraints = null, string? Description = null, FeatureType? Type = null, FeatureLifecycle? Lifecycle = null, List<Prerequisite>? Prerequisites = null, int? RequiredApprovals = null, bool? ProtectionEnabled = null, FeatureConfigurationState? FeatureState = null, EnvironmentConfigurationState? EnvironmentState = null);
public static partial class ConfigurationChanges
{
    public const string SnapshotField = "Configuration";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };

    public static ChangePayload Parse(ChangeRequestItem item)
    {
        ChangePayload payload;

        try
        {
            payload = JsonSerializer.Deserialize<ChangePayload>(item.Payload ?? "", Json) ?? throw new JsonException();
        }
        catch (JsonException)
        {
            throw new ArgumentException("ساختار داده قلم تغییر معتبر نیست.");
        }

        if (payload.Version != 1 || payload.ExpectedRevision < 0 || item.Action is not (ChangeActionNames.SetEnabled or ChangeActionNames.UpsertStrategy or ChangeActionNames.DeleteStrategy or ChangeActionNames.ReorderStrategies or ChangeActionNames.ReplaceVariants or ChangeActionNames.ReplaceSegmentConstraints or ChangeActionNames.UpdateFeature or ChangeActionNames.SetPrerequisites or ChangeActionNames.SetProjectProtection or ChangeActionNames.SetGlobalProtection or ChangeActionNames.RestoreFeature or ChangeActionNames.RestoreEnvironment))
        {
            throw new ArgumentException("نسخه، عمل یا شماره بازبینی قلم تغییر معتبر نیست.");
        }

        return payload;
    }

    public static IQueryable<FeatureStrategy> SegmentReferences(IApplicationDbContext db, int segmentId)
    {
        var json = JsonSerializer.Serialize(new[] { segmentId });

        return db.FeatureStrategies.FromSqlInterpolated($"SELECT * FROM \"App\".\"TblFeatureStrategy\" WHERE \"SegmentIds\" @> {json}::jsonb");
    }

    public static async Task<int> RequiredAsync(IApplicationDbContext db, Guid? projectId, Guid environmentId, CancellationToken ct)
    {
        var environment = await db.Environments.SingleOrDefaultAsync(e => e.Id == environmentId, ct) ?? throw new KeyNotFoundException("محیط یافت نشد.");
        var policy = await db.ProjectEnvironmentPolicies.SingleOrDefaultAsync(p => p.ProjectId == projectId && p.EnvironmentId == environmentId, ct);

        return Math.Max(environment.Protected ? 1 : 0, policy?.Enabled == true ? policy.RequiredApprovals : 0);
    }

    public static async Task EnsureOwnerAsync(IApplicationDbContext db, Guid? projectId, Guid actorId, bool globalApproval, CancellationToken ct)
    {
        var user = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == actorId && u.IsActive, ct);

        if (user is null)
        {
            throw new System.Security.SecurityException("حساب کاربری فعال نیست.");
        }

        if (!projectId.HasValue)
        {
            if (globalApproval ? user.Role != UserRole.RootAdmin : user.Role < UserRole.Admin)
            {
                throw new System.Security.SecurityException("دسترسی لازم برای تغییر سراسری ندارید.");
            }
        }
        else if (user.Role < UserRole.Admin && (user.Role < UserRole.Editor || !await db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == actorId && m.Role == ProjectRole.Owner, ct)))
        {
            throw new System.Security.SecurityException("دسترسی مالک پروژه لازم است.");
        }
    }

    /// <summary>Validates item payloads and captures revisions and protection scopes before review or execution.</summary>
    public static async Task<(Dictionary<string, long> Revisions, string Fingerprint)> InspectAsync(IApplicationDbContext db, ChangeRequest request, CancellationToken ct, bool checkExpected = true)
    {
        var revisions = new Dictionary<string, long>();
        var requestedRevisions = new Dictionary<string, long>();
        var affected = new List<string>();

        if (request.Items.Count == 0 || request.Items.Count > 100)
        {
            throw new ArgumentException("تعداد اقلام تغییر باید بین ۱ تا ۱۰۰ باشد.");
        }

        foreach (var item in request.Items)
        {
            var p = Parse(item);
            string key;
            long revision;

            if (item.Action == ChangeActionNames.ReplaceSegmentConstraints)
            {
                var segment = await db.Segments
                    .Include(s => s.Constraints)
                    .SingleOrDefaultAsync(s => s.Id == p.SegmentId, ct) ?? throw new KeyNotFoundException("بخش یافت نشد.");

                if (segment.ProjectId != request.ProjectId)
                {
                    throw new ArgumentException("بخش خارج از محدوده درخواست است.");
                }

                if (p.Constraints is null)
                {
                    throw new ArgumentException("فهرست قیود الزامی است.");
                }

                foreach (var c in p.Constraints)
                {
                    _ = Constraint.Create(c.ContextName, c.Operator, c.Values, c.Inverted, c.CaseInsensitive);
                }

                key = $"segment:{segment.Id}";
                revision = segment.Revision;

                var references = await SegmentReferences(db, segment.Id)
                    .Select(s => new { s.Id, s.Feature.ProjectId, s.EnvironmentId, s.Revision, EnvironmentRevision = s.FeatureEnvironment.Revision })
                    .ToListAsync(ct);

                affected.AddRange(references
                    .OrderBy(r => r.Id)
                    .Select(r => $"{r.Id}:{r.ProjectId}:{r.EnvironmentId}:{r.Revision}:{r.EnvironmentRevision}"));
            }
            else if (item.Action == ChangeActionNames.SetGlobalProtection)
            {
                if (request.ProjectId.HasValue || p.ProtectionEnabled is null)
                {
                    throw new ArgumentException("تغییر حفاظت سراسری باید در درخواست سراسری باشد.");
                }

                var env = await db.Environments.SingleAsync(e => e.Id == request.EnvironmentId, ct);

                key = $"environment:{env.Id}";
                revision = env.Revision;
            }
            else if (item.Action == ChangeActionNames.SetProjectProtection)
            {
                if (!request.ProjectId.HasValue || p.ProtectionEnabled is null || p.RequiredApprovals is not (>= 1 and <= 5))
                {
                    throw new ArgumentException("سیاست تأیید پروژه معتبر نیست.");
                }

                var policy = await db.ProjectEnvironmentPolicies.SingleOrDefaultAsync(x => x.ProjectId == request.ProjectId && x.EnvironmentId == request.EnvironmentId, ct);

                key = $"policy:{request.ProjectId}:{request.EnvironmentId}";
                revision = policy?.Revision ?? 0;
            }
            else
            {
                if (!request.ProjectId.HasValue || !item.FeatureId.HasValue)
                {
                    throw new ArgumentException("پرچم برای این عمل الزامی است.");
                }

                var env = await FeatureConfiguration.LoadAsync(db, item.FeatureId.Value, request.EnvironmentId, ct);

                if (env.Feature.ProjectId != request.ProjectId)
                {
                    throw new ArgumentException("پرچم خارج از پروژه درخواست است.");
                }

                var root = item.Action is ChangeActionNames.UpdateFeature or ChangeActionNames.SetPrerequisites or ChangeActionNames.RestoreFeature;

                key = root ? $"feature:{env.FeatureId}" : $"featureEnvironment:{env.Id}";
                revision = root ? env.Feature.Revision : env.Revision;

                if (item.Action is ChangeActionNames.RestoreFeature or ChangeActionNames.RestoreEnvironment)
                {
                    await ValidateRestoreAsync(db, env, item.Action, p, ct);
                }

                if (item.Action == ChangeActionNames.UpsertStrategy && p.Strategy is not null)
                {
                    if (p.Strategy.ExpectedRevision != p.ExpectedRevision)
                    {
                        throw new ArgumentException("بازبینی راهبرد باید با بازبینی قلم تغییر یکسان باشد.");
                    }

                    await FeatureConfiguration.ConfigureAsync(db, env, p.Strategy with
                    {
                        ExpectedRevision = env.Revision
                    }, ct, true);
                }

                if (item.Action == ChangeActionNames.SetPrerequisites && p.Prerequisites is not null)
                {
                    var projectFlags = await db.Features
                        .AsNoTracking()
                        .Where(f => f.ProjectId == request.ProjectId)
                        .ToListAsync(ct);

                    var graph = projectFlags.ToDictionary(f => f.Id, f => (IReadOnlyList<Prerequisite>)f.Prerequisites);

                    graph[env.FeatureId] = p.Prerequisites;
                    FlagEvaluator.ValidatePrerequisites(graph);
                }

                if (item.Action == ChangeActionNames.UpdateFeature && (p.Type.HasValue && !Enum.IsDefined(p.Type.Value) || p.Lifecycle.HasValue && p.Lifecycle is not (FeatureLifecycle.Active or FeatureLifecycle.Completed or FeatureLifecycle.Archived)))
                {
                    throw new ArgumentException("نوع یا مرحله عمر پرچم معتبر نیست.");
                }

                if (item.Action == ChangeActionNames.ReplaceVariants && p.StrategyId.HasValue && !env.Strategies.Any(s => s.Id == p.StrategyId))
                {
                    throw new KeyNotFoundException("راهبرد گونه‌ها یافت نشد.");
                }

                switch (item.Action)
                {
                    case ChangeActionNames.SetEnabled when p.Enabled is null:
                        throw new ArgumentException("وضعیت پرچم الزامی است.");
                    case ChangeActionNames.UpsertStrategy when p.Strategy is null:
                        throw new ArgumentException("پیکربندی راهبرد الزامی است.");
                    case ChangeActionNames.DeleteStrategy when !env.Strategies.Any(s => s.Id == p.StrategyId):
                        throw new ArgumentException("راهبرد یافت نشد.");
                    case ChangeActionNames.ReorderStrategies when p.StrategyIds is null || p.StrategyIds.Count != env.Strategies.Count || p.StrategyIds
                    .Distinct()
                    .Count() != p.StrategyIds.Count || env.Strategies.Any(s => !p.StrategyIds.Contains(s.Id)):
                        throw new ArgumentException("ترتیب راهبردها معتبر نیست.");
                    case ChangeActionNames.ReplaceVariants:
                        Variant.ValidateSet((p.Variants ?? throw new ArgumentException("گونه‌ها الزامی است."))
                            .Select(v => Variant.Create(v.Name, v.Weight, v.Payload, v.Stickiness))
                            .ToArray());
                        break;
                    case ChangeActionNames.SetPrerequisites when p.Prerequisites is null:
                        throw new ArgumentException("پیش‌نیازها الزامی است.");
                }
            }

            if (requestedRevisions.TryGetValue(key, out var previous) && previous != p.ExpectedRevision)
            {
                throw new ArgumentException("بازبینی اقلام یک منبع باید یکسان باشد.");
            }

            revisions[key] = revision;
            requestedRevisions[key] = p.ExpectedRevision;
            // A linked stage is validated against its predecessor's output when it executes.
            if (checkExpected)
            {
                FeatureConfiguration.RequireRevision(p.ExpectedRevision, revision);
            }
        }

        foreach (var scope in await AffectedScopesAsync(db, request, ct))
        {
            var environment = await db.Environments.SingleAsync(e => e.Id == scope.EnvironmentId, ct);
            var policy = await db.ProjectEnvironmentPolicies.SingleOrDefaultAsync(p => p.ProjectId == scope.ProjectId && p.EnvironmentId == scope.EnvironmentId, ct);

            affected.Add($"policy:{scope.ProjectId}:{scope.EnvironmentId}:{environment.Revision}:{policy?.Revision ?? 0}");
        }

        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', affected.Order(StringComparer.Ordinal)))));

        return (revisions, fingerprint);
    }

    private static async Task<List<(Guid? ProjectId, Guid EnvironmentId)>> AffectedScopesAsync(IApplicationDbContext db, ChangeRequest request, CancellationToken ct)
    {
        var scopes = new HashSet<(Guid?, Guid)>
        {
            (request.ProjectId, request.EnvironmentId)
        };

        foreach (var item in request.Items)
        {
            var payload = Parse(item);

            if (item.Action is ChangeActionNames.UpdateFeature or ChangeActionNames.SetPrerequisites or ChangeActionNames.RestoreFeature)
            {
                var environmentIds = await db.FeatureEnvironments
                    .Where(e => e.FeatureId == item.FeatureId)
                    .Select(e => e.EnvironmentId)
                    .ToListAsync(ct);

                foreach (var id in environmentIds)
                {
                    scopes.Add((request.ProjectId, id));
                }
            }

            if (item.Action == ChangeActionNames.ReplaceSegmentConstraints)
            {
                // SegmentIds is converted JSON; select reference metadata before matching in memory.
                var strategies = await SegmentReferences(db, payload.SegmentId!.Value)
                    .Select(s => new { s.SegmentIds, s.Feature.ProjectId, s.EnvironmentId })
                    .ToListAsync(ct);

                foreach (var reference in strategies.Where(s => s.SegmentIds.Contains(payload.SegmentId!.Value)))
                {
                    scopes.Add((reference.ProjectId, reference.EnvironmentId));
                }
            }
        }

        return scopes
            .OrderBy(s => s.Item1)
            .ThenBy(s => s.Item2)
            .Select(s => (s.Item1, s.Item2))
            .ToList();
    }

    internal static async Task<int> RequiredForRequestAsync(IApplicationDbContext db, ChangeRequest request, CancellationToken ct)
    {
        // Global scope uses the separately approved independent-RootAdmin rule.
        if (!request.ProjectId.HasValue)
        {
            return 1;
        }

        var required = 1;

        foreach (var scope in await AffectedScopesAsync(db, request, ct))
        {
            required = Math.Max(required, await RequiredAsync(db, scope.ProjectId, scope.EnvironmentId, ct));
        }

        return required;
    }

    public static async Task SubmitAsync(IApplicationDbContext db, ChangeRequest request, CancellationToken ct)
    {
        var (revisions, fingerprint) = await InspectAsync(db, request, ct);
        request.Capture(Math.Max(1, await RequiredForRequestAsync(db, request, ct)), revisions, fingerprint);
        request.SubmitForReview();
    }

    /// <summary>Revalidates approvals and resource revisions, then applies all items within the caller's transaction.</summary>
    public static async Task ApplyAsync(IApplicationDbContext db, ChangeRequest request, Guid actorId, CancellationToken ct)
    {
        if (request.Status == ChangeRequestStatus.Applied)
        {
            return;
        }

        await EnsureOwnerAsync(db, request.ProjectId, actorId, !request.ProjectId.HasValue, ct);

        if (request.Status is not (ChangeRequestStatus.Approved or ChangeRequestStatus.Scheduled))
        {
            throw new InvalidEntityStateException("درخواست برای اجرا تأیید نشده است.");
        }

        var required = Math.Max(request.RequiredApprovals, Math.Max(1, await RequiredForRequestAsync(db, request, ct)));
        var approvals = request.Reviewers
            .Where(id => id != request.CreatedBy)
            .Distinct()
            .ToArray();

        if (approvals.Length < required)
        {
            throw new InvalidEntityStateException("تعداد تأییدها کافی نیست.");
        }

        foreach (var reviewer in approvals)
        {
            await EnsureOwnerAsync(db, request.ProjectId, reviewer, !request.ProjectId.HasValue, ct);
        }

        var (current, fingerprint) = await InspectAsync(db, request, ct, false);

        if (fingerprint != request.ScopeFingerprint)
        {
            throw new DbUpdateConcurrencyException("محدوده منابع درخواست تغییر کرده است.");
        }

        var expected = new Dictionary<string, long>(request.CapturedRevisions);

        if (request.PredecessorId.HasValue)
        {
            var predecessor = await db.ChangeRequests.SingleAsync(r => r.Id == request.PredecessorId, ct);

            if (predecessor.Status != ChangeRequestStatus.Applied)
            {
                throw new InvalidEntityStateException("مرحله پیشین هنوز با موفقیت اجرا نشده است.");
            }

            foreach (var predecessorResult in predecessor.ResultRevisions.Where(p => expected.ContainsKey(p.Key)))
            {
                expected[predecessorResult.Key] = predecessorResult.Value;
            }
        }

        foreach (var value in current)
        {
            FeatureConfiguration.RequireRevision(expected.GetValueOrDefault(value.Key, -1), value.Value);
        }

        foreach (var item in request.Items)
        {
            await ApplyItemAsync(db, request, item, ct);
        }

        // Save target revisions and history first inside the caller's transaction; status remains invisible until commit.
        await db.SaveChangeAsync(ct);

        var result = await InspectAfterAsync(db, request, ct);

        request.RecordResult(result);
        request.Apply();

        await db.SaveChangeAsync(ct);
    }

    private static async Task<Dictionary<string, long>> InspectAfterAsync(IApplicationDbContext db, ChangeRequest request, CancellationToken ct)
    {
        var result = new Dictionary<string, long>();

        foreach (var key in request.CapturedRevisions.Keys)
        {
            var parts = key.Split(':');

            result[key] = parts[0] switch
            {
                "feature" => await db.Features
                .Where(f => f.Id == Guid.Parse(parts[1]))
                .Select(f => f.Revision)
                .SingleAsync(ct),
                "featureEnvironment" => await db.FeatureEnvironments
                .Where(f => f.Id == Guid.Parse(parts[1]))
                .Select(f => f.Revision)
                .SingleAsync(ct),
                "segment" => await db.Segments
                .Where(f => f.Id == int.Parse(parts[1]))
                .Select(f => f.Revision)
                .SingleAsync(ct),
                "environment" => await db.Environments
                .Where(f => f.Id == Guid.Parse(parts[1]))
                .Select(f => f.Revision)
                .SingleAsync(ct),
                "policy" => await db.ProjectEnvironmentPolicies
                .Where(f => f.ProjectId == request.ProjectId && f.EnvironmentId == request.EnvironmentId)
                .Select(f => f.Revision)
                .SingleAsync(ct),
                _ => throw new ArgumentException("نوع منبع تغییر معتبر نیست.")
            };
        }

        return result;
    }

    private static async Task ApplyItemAsync(IApplicationDbContext db, ChangeRequest request, ChangeRequestItem item, CancellationToken ct)
    {
        var p = Parse(item);

        if (item.Action == ChangeActionNames.ReplaceSegmentConstraints)
        {
            var segment = await db.Segments
                .Include(s => s.Constraints)
                .SingleAsync(s => s.Id == p.SegmentId, ct);

            db.SegmentConstraints.RemoveRange(segment.Constraints);
            segment.UpdateConstraints(p.Constraints!
                .Select(c => Constraint.Create(c.ContextName, c.Operator, c.Values, c.Inverted, c.CaseInsensitive))
                .ToList());

            return;
        }

        if (item.Action == ChangeActionNames.SetGlobalProtection)
        {
            (await db.Environments.SingleAsync(e => e.Id == request.EnvironmentId, ct)).SetProtected(p.ProtectionEnabled!.Value);

            return;
        }

        if (item.Action == ChangeActionNames.SetProjectProtection)
        {
            var policy = await db.ProjectEnvironmentPolicies.SingleOrDefaultAsync(x => x.ProjectId == request.ProjectId && x.EnvironmentId == request.EnvironmentId, ct);

            if (policy is null)
            {
                policy = new ProjectEnvironmentPolicy
                {
                    ProjectId = request.ProjectId!.Value,
                    EnvironmentId = request.EnvironmentId
                };
                db.ProjectEnvironmentPolicies.Add(policy);
            }

            policy.Enabled = p.ProtectionEnabled!.Value;
            policy.RequiredApprovals = p.RequiredApprovals!.Value;

            return;
        }

        var environment = await FeatureConfiguration.LoadAsync(db, item.FeatureId!.Value, request.EnvironmentId, ct);

        switch (item.Action)
        {
            case ChangeActionNames.RestoreFeature:
                var state = p.FeatureState!;
                environment.Feature.RestoreConfiguration(state.Description, state.Type, state.Lifecycle, state.IsStale, state.ImpressionDataEnabled, state.Tags, state.Prerequisites);

                var restoredFlags = await db.Features
                    .Where(f => f.ProjectId == request.ProjectId)
                    .ToListAsync(ct);

                FlagEvaluator.ValidatePrerequisites(restoredFlags.ToDictionary(f => f.Id, f => (IReadOnlyList<Prerequisite>)f.Prerequisites));
                break;
            case ChangeActionNames.RestoreEnvironment:
                await RestoreEnvironmentAsync(db, environment, p.EnvironmentState!, ct);
                break;
            case ChangeActionNames.SetEnabled:
                if (p.Enabled!.Value)
                {
                    environment.Enable();
                }
                else
                {
                    environment.Disable();
                }

                break;
            case ChangeActionNames.UpsertStrategy:
                await FeatureConfiguration.ConfigureAsync(db, environment, p.Strategy! with
                {
                    FeatureId = item.FeatureId.Value,
                    EnvironmentId = request.EnvironmentId,
                    ExpectedRevision = environment.Revision
                }, ct);
                break;
            case ChangeActionNames.DeleteStrategy:
                environment.RemoveStrategy(p.StrategyId!.Value);
                break;
            case ChangeActionNames.ReorderStrategies:
                for (var i = 0; i < p.StrategyIds!.Count; i++)
                {
                    environment.Strategies
                        .Single(s => s.Id == p.StrategyIds[i])
                        .UpdateSortOrder(i);
                }

                break;
            case ChangeActionNames.ReplaceVariants:
                var variants = p.Variants!
                    .Select(v => Variant.Create(v.Name, v.Weight, v.Payload, v.Stickiness))
                    .ToList();
                if (p.StrategyId.HasValue)
                {
                    db.FeatureVariants.RemoveRange(environment.Strategies.Single(s => s.Id == p.StrategyId).Variants);
                    environment.Strategies
                        .Single(s => s.Id == p.StrategyId)
                        .SetVariants(variants);
                }
                else
                {
                    db.FeatureVariants.RemoveRange(environment.Variants);
                    environment.SetVariants(variants);
                }

                break;
            case ChangeActionNames.UpdateFeature:
                environment.Feature.Update(p.Description, p.Type);
                if (p.Lifecycle is not null)
                {
                    switch (p.Lifecycle)
                    {
                        case FeatureLifecycle.Active:
                            environment.Feature.Activate();
                            break;
                        case FeatureLifecycle.Completed:
                            environment.Feature.Complete();
                            break;
                        case FeatureLifecycle.Archived:
                            environment.Feature.Archive();
                            break;
                        default:
                            throw new ArgumentException("مرحله عمر پرچم معتبر نیست.");
                    }
                }

                break;
            case ChangeActionNames.SetPrerequisites:
                var flags = await db.Features
                    .Where(f => f.ProjectId == request.ProjectId)
                    .ToListAsync(ct);
                if (p.Prerequisites!.Any(parent => !flags.Any(f => f.Id == parent.FeatureId)))
                {
                    throw new ArgumentException("پیش‌نیاز خارج از پروژه است.");
                }

                environment.Feature.SetPrerequisites(p.Prerequisites);
                FlagEvaluator.ValidatePrerequisites(flags.ToDictionary(f => f.Id, f => (IReadOnlyList<Prerequisite>)f.Prerequisites));
                break;
        }

        environment.ConfigurationChanged();
    }

    public static async Task SuspendSuccessorsAsync(IApplicationDbContext db, Guid predecessorId, CancellationToken ct)
    {
        var pending = new Queue<Guid>();

        pending.Enqueue(predecessorId);

        var visited = new HashSet<Guid>();

        while (pending.Count > 0)
        {
            var id = pending.Dequeue();

            if (!visited.Add(id))
            {
                continue;
            }

            var successors = await db.ChangeRequests
                .Where(r => r.PredecessorId == id && r.Status != ChangeRequestStatus.Applied && r.Status != ChangeRequestStatus.Cancelled)
                .ToListAsync(ct);

            foreach (var successor in successors)
            {
                successor.Suspend("مرحله پیشین تغییر کرده یا اجرا نشده است؛ بررسی مجدد لازم است.");
                pending.Enqueue(successor.Id);
            }
        }
    }
}
