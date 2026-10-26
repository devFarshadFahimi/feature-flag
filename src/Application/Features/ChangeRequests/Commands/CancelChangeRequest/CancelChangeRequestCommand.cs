namespace Application.Features.ChangeRequests.Commands.CancelChangeRequest;

public record CancelChangeRequestCommand(Guid Id) : ICommandRequest;
internal class CancelChangeRequestCommandHandler(IApplicationDbContext dbContext) : CommandRequestHandler<CancelChangeRequestCommand>
{
    public override async Task<Result> Handle(CancelChangeRequestCommand request, CancellationToken cancellationToken)
    {
        var changeRequest = await dbContext.ChangeRequests.FindAsync([request.Id], cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        changeRequest.Cancel();
        await ConfigurationChanges.SuspendSuccessorsAsync(dbContext, changeRequest.Id, cancellationToken);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok();
    }
}