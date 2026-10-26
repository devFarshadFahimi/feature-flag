using Application.Common.Services;
using Application.Features.ChangeRequests;
using Domain.Aggregates.ChangeRequestAggregate;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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


        using var json = JsonDocument.Parse(history.Before);
        var actor = Guid.Parse(User.FindFirst("sub")!.Value);
        var request = ChangeRequest.Create(input.ProjectId, input.EnvironmentId, actor, "بازیابی از تاریخچه", null);
        string action;
        Guid featureId;
        ChangePayload payload;

        if (history.ResourceType == "FeatureEnvironment")
        {
            if (history.EnvironmentId != input.EnvironmentId)
            {
                throw new ArgumentException("محیط بازیابی باید با محیط رکورد تاریخچه یکسان باشد.");
            }


            featureId = json.RootElement
                .GetProperty("FeatureId")
                .GetGuid();
            action = "setEnabled";
            payload = new(1, input.ExpectedRevision, Enabled: json.RootElement
                .GetProperty("Enabled")
                .GetBoolean());
        }
        else if (history.ResourceType == "Feature")
        {
            featureId = Guid.Parse(history.ResourceId);
            action = "updateFeature";

            var type = (FeatureType)json.RootElement
                .GetProperty("Type")
                .GetInt32();

            payload = new(1, input.ExpectedRevision, Description: json.RootElement
                .GetProperty("Description")
                .GetString(), Type: type);
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
}

public record RestoreHistoryInput(Guid ProjectId, Guid EnvironmentId, long ExpectedRevision);