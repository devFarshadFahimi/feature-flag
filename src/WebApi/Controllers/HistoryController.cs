using System.Text.Json;
using Application.Common.Services;
using Application.Features.ChangeRequests;
using Application.Features.FeatureEnvironments;
using Application.Features.Segments.Commands.CreateSegment;
using Domain.Aggregates.ChangeRequestAggregate;
using Domain.Aggregates.FeatureAggregate;
using Domain.Aggregates.ProjectAggregate;
using Domain.Aggregates.SegmentAggregate;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using FlagEnvironment = Domain.Aggregates.EnvironmentAggregate.Environment;

namespace WebApi.Controllers;

[Authorize]
public sealed class HistoryController(IMediator mediator, IApplicationDbContext db) : ApiControllerBase(mediator)
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid? projectId, Guid? environmentId = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (page is < 1 or > 100000 || pageSize is < 1 or > 200)
        {
            return BadRequest(new
            {
                Message = "صفحه‌بندی معتبر نیست."
            });
        }

        var query = db.ConfigurationHistory
            .AsNoTracking()
            .Where(h => h.ProjectId == projectId && (!environmentId.HasValue || h.EnvironmentId == environmentId));

        return Ok(await query
            .OrderByDescending(h => h.Timestamp)
            .ThenBy(h => h.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken));
    }

    /// <summary>Creates a revision-checked draft; restoration always requires the ordinary review and apply workflow.</summary>
    /// <param name="id">The identifier of the history record whose before-image should be restored.</param>
    /// <param name="input">The configuration values to validate and apply.</param>
    /// <param name="cancellationToken">Cancels the operation and its database or network requests.</param>
    /// <returns>The newly created draft change request, ready for the ordinary review workflow.</returns>
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, [FromBody] RestoreHistoryInput input, CancellationToken cancellationToken)
    {
        var history = await db.ConfigurationHistory
            .AsNoTracking()
            .SingleOrDefaultAsync(h => h.Id == id, cancellationToken) ?? throw new KeyNotFoundException("رکورد تاریخچه یافت نشد.");

        if (history.ProjectId != input.ProjectId || history.Before is null)
        {
            throw new ArgumentException("بازیابی این رکورد برای پروژه ممکن نیست.");
        }

        if (!await db.Environments.AnyAsync(environment => environment.Id == input.EnvironmentId, cancellationToken))
        {
            throw new KeyNotFoundException("محیط بازیابی یافت نشد؛ تصویر ناقص برای بازسازی محیط حذف‌شده کافی نیست.");
        }

        using var json = JsonDocument.Parse(history.Before);
        var actor = Guid.Parse(User.FindFirst(Application.Common.Services.IdentityClaimNames.Subject)!.Value);
        var request = input.ProjectId.HasValue ? ChangeRequest.Create(input.ProjectId.Value, input.EnvironmentId, actor, "بازیابی از تاریخچه", null) : ChangeRequest.CreateGlobal(input.EnvironmentId, actor, "بازیابی از تاریخچه", null);
        string action;
        Guid? featureId = null;
        ChangePayload payload;
        var snapshot = json.RootElement;
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        if (history.ResourceType == nameof(FeatureEnvironment))
        {
            if (history.EnvironmentId != input.EnvironmentId)
            {
                throw new ArgumentException("محیط بازیابی باید با محیط رکورد تاریخچه یکسان باشد.");
            }

            featureId = json.RootElement
                .GetProperty(nameof(FeatureEnvironment.FeatureId))
                .GetGuid();

            if (snapshot.TryGetProperty(ConfigurationChanges.SnapshotField, out var configuration))
            {
                action = ChangeActionNames.RestoreEnvironment;
                payload = new(1, input.ExpectedRevision, EnvironmentState: configuration.Deserialize<EnvironmentConfigurationState>(options));
            }
            else
            {
                action = ChangeActionNames.SetEnabled;
                payload = new(1, input.ExpectedRevision, Enabled: snapshot
                    .GetProperty(nameof(FeatureEnvironment.Enabled))
                    .GetBoolean());
            }
        }
        else if (history.ResourceType == nameof(Feature))
        {
            featureId = Guid.Parse(history.ResourceId);
            action = ChangeActionNames.UpdateFeature;

            var type = (FeatureType)json.RootElement
                .GetProperty(nameof(Feature.Type))
                .GetInt32();

            payload = new(1, input.ExpectedRevision, Description: json.RootElement
                .GetProperty(nameof(Feature.Description))
                .GetString(), Type: type);

            if (snapshot.TryGetProperty(ConfigurationChanges.SnapshotField, out var configuration))
            {
                action = ChangeActionNames.RestoreFeature;
                payload = new(1, input.ExpectedRevision, FeatureState: configuration.Deserialize<FeatureConfigurationState>(options));
            }
        }
        else if (history.ResourceType == nameof(FeatureStrategy))
        {
            RequireEnvironment(history.EnvironmentId, input.EnvironmentId);

            var configuration = ReadConfiguration(snapshot).Deserialize<ConfigureStrategyCommand>(options) ?? throw new ArgumentException("تصویر راهبرد کامل نیست.");

            featureId = configuration.FeatureId;

            var strategyId = await db.FeatureStrategies.AnyAsync(s => s.Id == configuration.StrategyId, cancellationToken) ? configuration.StrategyId : null;

            action = ChangeActionNames.UpsertStrategy;
            var strategyInput = configuration with
            {
                StrategyId = strategyId,
                ExpectedRevision = input.ExpectedRevision
            };

            payload = new(1, input.ExpectedRevision, Strategy: strategyInput);
        }
        else if (history.ResourceType == nameof(Segment))
        {
            action = ChangeActionNames.RestoreSegment;
            var name = snapshot
                .GetProperty(nameof(Segment.Name))
                .GetString()!;
            var description = snapshot
                .GetProperty(nameof(Segment.Description))
                .GetString();
            var isPublic = snapshot
                .GetProperty(nameof(Segment.IsPublic))
                .GetBoolean();
            var segmentState = new SegmentConfigurationState(
                name,
                description,
                isPublic,
                history.ProjectId,
                ReadConfiguration(snapshot).Deserialize<List<ConstraintDto>>(options)!);

            payload = new(1, input.ExpectedRevision, SegmentId: int.Parse(history.ResourceId), SegmentState: segmentState);
        }
        else if (history.ResourceType == nameof(Project))
        {
            if (!input.ProjectId.HasValue || Guid.Parse(history.ResourceId) != input.ProjectId.Value)
            {
                throw new ArgumentException("پروژه بازیابی با رکورد تاریخچه یکسان نیست.");
            }

            action = ChangeActionNames.RestoreProject;
            var name = snapshot
                .GetProperty(nameof(Project.Name))
                .GetString()!;
            var description = snapshot
                .GetProperty(nameof(Project.Description))
                .GetString()!;
            var defaultStickiness = snapshot
                .GetProperty(nameof(Project.DefaultStickiness))
                .GetString()!;
            var limitEnabled = snapshot
                .GetProperty(nameof(Project.FeatureLimitEnabled))
                .GetBoolean();
            var featureLimit = limitEnabled
                ? snapshot
                    .GetProperty(nameof(Project.FeatureLimit))
                    .GetInt32()
                : (int?)null;
            var projectState = new ProjectConfigurationState(
                name,
                description,
                defaultStickiness,
                featureLimit);

            payload = new(1, input.ExpectedRevision, ProjectState: projectState);
        }
        else if (history.ResourceType == nameof(ProjectEnvironmentPolicy))
        {
            RequireEnvironment(history.EnvironmentId, input.EnvironmentId);
            action = ChangeActionNames.SetProjectProtection;
            payload = new(1, input.ExpectedRevision, RequiredApprovals: snapshot
                .GetProperty(nameof(ProjectEnvironmentPolicy.RequiredApprovals))
                .GetInt32(), ProtectionEnabled: snapshot
                .GetProperty(nameof(FlagEnvironment.Enabled))
                .GetBoolean());
        }
        else if (history.ResourceType == nameof(Domain.Aggregates.EnvironmentAggregate.Environment))
        {
            RequireEnvironment(history.EnvironmentId, input.EnvironmentId);
            action = ChangeActionNames.RestoreGlobalEnvironment;
            var name = snapshot
                .GetProperty(nameof(FlagEnvironment.Name))
                .GetString()!;
            var sortOrder = snapshot
                .GetProperty(nameof(FlagEnvironment.SortOrder))
                .GetInt32();
            var enabled = snapshot
                .GetProperty(nameof(FlagEnvironment.Enabled))
                .GetBoolean();
            var isProtected = snapshot
                .GetProperty(nameof(FlagEnvironment.Protected))
                .GetBoolean();
            var environmentState = new GlobalEnvironmentState(
                name,
                sortOrder,
                enabled,
                isProtected);

            payload = new(1, input.ExpectedRevision, GlobalEnvironmentState: environmentState);
        }
        else
        {
            throw new ArgumentException("این نوع رکورد باید با قلم تغییر صریح بازیابی شود.");
        }

        request.AddItem(ChangeRequestItem.Create(action, featureId, JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))));
        _ = await ConfigurationChanges.InspectAsync(db, request, cancellationToken);
        db.ChangeRequests.Add(request);

        await db.SaveChangeAsync(cancellationToken);

        return Ok(request.Id);
    }

    private static JsonElement ReadConfiguration(JsonElement snapshot)
    {
        if (!snapshot.TryGetProperty(ConfigurationChanges.SnapshotField, out var configuration))
        {
            throw new ArgumentException("رکورد قدیمی تصویر کامل پیکربندی ندارد؛ قلم تغییر صریح لازم است.");
        }

        return configuration;
    }

    private static void RequireEnvironment(Guid? actual, Guid requested)
    {
        if (actual != requested)
        {
            throw new ArgumentException("محیط بازیابی باید با محیط رکورد تاریخچه یکسان باشد.");
        }
    }
}

public record RestoreHistoryInput(Guid? ProjectId, Guid EnvironmentId, long ExpectedRevision);
