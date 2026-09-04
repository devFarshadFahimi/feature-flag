namespace Application.Features.ChangeRequests.Commands.ApplyChangeRequest;

public record ApplyChangeRequestCommand(Guid Id) : ICommandRequest;

internal class ApplyChangeRequestCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<ApplyChangeRequestCommand>
{
    public override async Task<Result> Handle(ApplyChangeRequestCommand request, CancellationToken cancellationToken)
    {
        var changeRequest = await dbContext.ChangeRequests.FindAsync([request.Id], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(ChangeRequest), request.Id + string.Empty);

        changeRequest.Apply();
        // TODO: Apply all items to the actual features
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}