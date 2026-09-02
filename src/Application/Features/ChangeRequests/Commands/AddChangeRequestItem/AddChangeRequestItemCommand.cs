using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.ChangeRequests;
using Domain.Exceptions;
namespace Application.Features.ChangeRequests.Commands.AddChangeRequestItem;

public record AddChangeRequestItemCommand(
    Guid ChangeRequestId,
    string Action,
    Guid? FeatureId = null,
    string? Payload = null) : ICommandRequest<Guid>;

internal class AddChangeRequestItemCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<AddChangeRequestItemCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(AddChangeRequestItemCommand request, CancellationToken cancellationToken)
{
    var changeRequest = await dbContext.ChangeRequests.FindAsync([request.ChangeRequestId], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ChangeRequest), request.ChangeRequestId);

    var item = ChangeRequestItem.Create(request.Action, request.FeatureId, request.Payload);
    changeRequest.AddItem(item);
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok(item.Id);
}
}