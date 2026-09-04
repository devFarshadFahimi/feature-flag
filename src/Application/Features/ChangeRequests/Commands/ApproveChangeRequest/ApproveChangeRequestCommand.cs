namespace Application.Features.ChangeRequests.Commands.ApproveChangeRequest;

public record ApproveChangeRequestCommand(Guid Id, Guid ReviewerId) : ICommandRequest;

internal class ApproveChangeRequestCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<ApproveChangeRequestCommand>
{
    public override async Task<Result> Handle(ApproveChangeRequestCommand request, CancellationToken cancellationToken)
    {
        var changeRequest = await dbContext.ChangeRequests.FindAsync([request.Id], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(ChangeRequest), request.Id + string.Empty);

        changeRequest.Approve(request.ReviewerId);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}