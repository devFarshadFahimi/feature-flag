using Application.Features.Segments.Commands.AddSegmentConstraint;
using Application.Features.Segments.Commands.CreateSegment;
using Application.Features.Segments.Commands.DeleteSegment;
using Application.Features.Segments.Commands.RemoveSegmentConstraint;
using Application.Features.Segments.Commands.UpdateSegment;
using Application.Features.Segments.Queries.GetAllSegments;
using Application.Features.Segments.Queries.GetSegmentById;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Authorize]
public class SegmentsController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSegmentCommand command, CancellationToken cancellationToken)
    {
        return await SendAsync(command, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return await SendAsync(new GetAllSegmentsQuery(), cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return await SendAsync(new GetSegmentByIdQuery(id), cancellationToken);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSegmentCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { Message = "Id mismatch" });
        }

        return await SendAsync(command, cancellationToken);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        return await SendAsync(new DeleteSegmentCommand(id), cancellationToken);
    }

    [HttpPost("{id:int}/constraints")]
    public async Task<IActionResult> AddConstraint(int id, [FromBody] AddSegmentConstraintCommand command, CancellationToken cancellationToken)
    {
        if (id != command.SegmentId)
        {
            return BadRequest(new { Message = "Segment id mismatch" });
        }

        return await SendAsync(command, cancellationToken);
    }

    [HttpDelete("{id:int}/constraints/{index:int}")]
    public async Task<IActionResult> RemoveConstraint(int id, int index, CancellationToken cancellationToken)
    {
        return await SendAsync(new RemoveSegmentConstraintCommand(id, index), cancellationToken);
    }
}