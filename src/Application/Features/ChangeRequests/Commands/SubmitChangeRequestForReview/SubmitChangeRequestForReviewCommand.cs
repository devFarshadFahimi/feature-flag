namespace Application.Features.ChangeRequests.Commands.SubmitChangeRequestForReview;

public record SubmitChangeRequestForReviewCommand(Guid Id) : ICommandRequest;
internal class SubmitChangeRequestForReviewCommandHandler(IApplicationDbContext dbContext, BusinessMakerFramework.Shared.Abstractions.UserManagement.IUserInfoService actor) : CommandRequestHandler<SubmitChangeRequestForReviewCommand>
{
    public override async Task<Result> Handle(SubmitChangeRequestForReviewCommand request, CancellationToken cancellationToken)
    {
        var changeRequest = await dbContext.ChangeRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        await ConfigurationChanges.SubmitAsync(dbContext, changeRequest, cancellationToken);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok();
    }
}