namespace Application.Features.ChangeRequests.Commands.ApproveChangeRequest;

public record ApproveChangeRequestCommand(Guid Id, Guid ReviewerId) : ICommandRequest;
internal class ApproveChangeRequestCommandHandler(IApplicationDbContext dbContext, BusinessMakerFramework.Shared.Abstractions.UserManagement.IUserInfoService actor) : CommandRequestHandler<ApproveChangeRequestCommand>
{
    public override async Task<Result> Handle(ApproveChangeRequestCommand request, CancellationToken cancellationToken)
    {
        var changeRequest = await dbContext.ChangeRequests.FindAsync([request.Id], cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        await ConfigurationChanges.EnsureOwnerAsync(dbContext, changeRequest.ProjectId, actor.Id(), !changeRequest.ProjectId.HasValue, cancellationToken);
        changeRequest.Approve(actor.Id());

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok();
    }
}