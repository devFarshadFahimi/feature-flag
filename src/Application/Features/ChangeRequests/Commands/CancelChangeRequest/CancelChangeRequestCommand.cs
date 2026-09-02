using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.ChangeRequests;
using Domain.Exceptions;
namespace Application.Features.ChangeRequests.Commands.CancelChangeRequest;

public record CancelChangeRequestCommand(Guid Id) : ICommandRequest;

internal class CancelChangeRequestCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<CancelChangeRequestCommand>
{
    public override async Task<Result> Handle(CancelChangeRequestCommand request, CancellationToken cancellationToken)
{
    var changeRequest = await dbContext.ChangeRequests.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ChangeRequest), request.Id);

    changeRequest.Cancel();
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}