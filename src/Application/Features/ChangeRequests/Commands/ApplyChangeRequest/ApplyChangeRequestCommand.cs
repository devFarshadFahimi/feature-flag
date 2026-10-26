namespace Application.Features.ChangeRequests.Commands.ApplyChangeRequest;

public record ApplyChangeRequestCommand(Guid Id) : ICommandRequest;
internal class ApplyChangeRequestCommandHandler(IApplicationDbContext dbContext, BusinessMakerFramework.Shared.Abstractions.UserManagement.IUserInfoService actor) : CommandRequestHandler<ApplyChangeRequestCommand>
{
    public override async Task<Result> Handle(ApplyChangeRequestCommand request, CancellationToken cancellationToken)
    {
        var changeRequest = await dbContext.ChangeRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        await ConfigurationChanges.ApplyAsync(dbContext, changeRequest, actor.Id(), cancellationToken);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok();
    }
}