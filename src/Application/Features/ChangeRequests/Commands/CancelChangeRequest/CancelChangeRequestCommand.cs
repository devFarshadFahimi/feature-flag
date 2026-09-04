namespace Application.Features.ChangeRequests.Commands.CancelChangeRequest;

public record CancelChangeRequestCommand(Guid Id) : ICommandRequest;

internal class CancelChangeRequestCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<CancelChangeRequestCommand>
{
    public override async Task<Result> Handle(CancelChangeRequestCommand request, CancellationToken cancellationToken)
    {
        var changeRequest = await dbContext.ChangeRequests.FindAsync([request.Id], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(ChangeRequest), request.Id + string.Empty);

        changeRequest.Cancel();
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}