using Application.Features.ChangeRequests.Commands.AddChangeRequestItem;
using Application.Features.ChangeRequests.Commands.ApplyChangeRequest;
using Application.Features.ChangeRequests.Commands.ApproveChangeRequest;
using Application.Features.ChangeRequests.Commands.CancelChangeRequest;
using Application.Features.ChangeRequests.Commands.CreateChangeRequest;
using Application.Features.ChangeRequests.Commands.RejectChangeRequest;
using Application.Features.ChangeRequests.Commands.ScheduleChangeRequest;
using Application.Features.ChangeRequests.Commands.SubmitChangeRequestForReview;
using Application.Features.ChangeRequests.Queries.GetAllChangeRequests;
using Application.Features.ChangeRequests.Queries.GetChangeRequestById;

namespace WebApi.Controllers;

[Authorize]
public class ChangeRequestsController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChangeRequestCommand command, CancellationToken cancellationToken)
    {
        return await SendAsync(command, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? projectId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        return await SendAsync(new GetAllChangeRequestsQuery(projectId, status), cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new GetChangeRequestByIdQuery(id), cancellationToken);
    }

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddChangeRequestItemCommand command, CancellationToken cancellationToken)
    {
        return id != command.ChangeRequestId
            ? BadRequest(new { Message = "Change request id mismatch" })
            : await SendAsync(command, cancellationToken);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> SubmitForReview(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new SubmitChangeRequestForReviewCommand(id), cancellationToken);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveChangeRequestCommand command, CancellationToken cancellationToken)
    {
        return id != command.Id ? BadRequest(new { Message = "Id mismatch" }) : await SendAsync(command, cancellationToken);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectChangeRequestCommand command, CancellationToken cancellationToken)
    {
        return id != command.Id ? BadRequest(new { Message = "Id mismatch" }) : await SendAsync(command, cancellationToken);
    }

    [HttpPost("{id:guid}/apply")]
    public async Task<IActionResult> Apply(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new ApplyChangeRequestCommand(id), cancellationToken);
    }

    [HttpPost("{id:guid}/schedule")]
    public async Task<IActionResult> Schedule(Guid id, [FromBody] ScheduleChangeRequestCommand command, CancellationToken cancellationToken)
    {
        return id != command.Id ? BadRequest(new { Message = "Id mismatch" }) : await SendAsync(command, cancellationToken);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(new CancelChangeRequestCommand(id), cancellationToken);
    }
}