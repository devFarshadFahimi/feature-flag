using Application.Features.Features.Commands.AddFeatureTag;
using Application.Features.Features.Commands.ArchiveFeature;
using Application.Features.Features.Commands.CreateFeature;
using Application.Features.Features.Commands.EnableFeatureImpressionData;
using Application.Features.Features.Commands.MarkFeatureAsStale;
using Application.Features.Features.Commands.RemoveFeatureTag;
using Application.Features.Features.Commands.UpdateFeature;
using Application.Features.Features.Queries.GetAllFeatures;
using Application.Features.Features.Queries.GetFeatureById;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Authorize]
public class FeaturesController(IMediator mediator) : BusinessApiControllerBase(mediator)
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFeatureCommand command, CancellationToken cancellationToken)
    {
        return await SendAsync(command, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? projectId, CancellationToken cancellationToken)
    {
        return await SendAsync(new GetAllFeaturesQuery(projectId), cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new GetFeatureByIdQuery(id), cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFeatureCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest(new { Message = "Id mismatch" });
        return await SendAsync(command, cancellationToken);
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new ArchiveFeatureCommand(id), cancellationToken);
    }

    [HttpPut("{id:guid}/stale")]
    public async Task<IActionResult> MarkAsStale(Guid id, [FromBody] MarkFeatureAsStaleCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest(new { Message = "Id mismatch" });
        return await SendAsync(command, cancellationToken);
    }

    [HttpPut("{id:guid}/impression-data")]
    public async Task<IActionResult> EnableImpressionData(Guid id, [FromBody] EnableFeatureImpressionDataCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest(new { Message = "Id mismatch" });
        return await SendAsync(command, cancellationToken);
    }

    [HttpPost("{id:guid}/tags")]
    public async Task<IActionResult> AddTag(Guid id, [FromBody] AddFeatureTagCommand command, CancellationToken cancellationToken)
    {
        if (id != command.FeatureId)
            return BadRequest(new { Message = "Feature id mismatch" });
        return await SendAsync(command, cancellationToken);
    }

    [HttpDelete("{id:guid}/tags/{tag}")]
    public async Task<IActionResult> RemoveTag(Guid id, string tag, CancellationToken cancellationToken)
    {
        return await SendAsync(new RemoveFeatureTagCommand(id, tag), cancellationToken);
    }
}