namespace Application.Features.ChangeRequests.Commands.RejectChangeRequest;

public record RejectChangeRequestCommand(Guid Id, Guid ReviewerId, string? Reason = null) : ICommandRequest;

internal class RejectChangeRequestCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<RejectChangeRequestCommand>
{
    public override async Task<Result> Handle(RejectChangeRequestCommand request, CancellationToken cancellationToken)
    {
        var changeRequest = await dbContext.ChangeRequests.FindAsync([request.Id], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(ChangeRequest), request.Id + string.Empty);

        changeRequest.Reject(request.ReviewerId, request.Reason);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}