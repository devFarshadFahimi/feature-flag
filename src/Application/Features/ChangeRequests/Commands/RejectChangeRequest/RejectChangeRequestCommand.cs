namespace Application.Features.ChangeRequests.Commands.RejectChangeRequest;

public record RejectChangeRequestCommand(Guid Id, Guid ReviewerId, string? Reason = null) : ICommandRequest;
internal class RejectChangeRequestCommandHandler(IApplicationDbContext dbContext, BusinessMakerFramework.Shared.Abstractions.UserManagement.IUserInfoService actor) : CommandRequestHandler<RejectChangeRequestCommand>
{
    public override async Task<Result> Handle(RejectChangeRequestCommand request, CancellationToken cancellationToken)
    {
        var changeRequest = await dbContext.ChangeRequests.FindAsync([request.Id], cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        await ConfigurationChanges.EnsureOwnerAsync(dbContext, changeRequest.ProjectId, actor.Id(), !changeRequest.ProjectId.HasValue, cancellationToken);
        changeRequest.Reject(actor.Id(), request.Reason);
        await ConfigurationChanges.SuspendSuccessorsAsync(dbContext, changeRequest.Id, cancellationToken);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok();
    }
}