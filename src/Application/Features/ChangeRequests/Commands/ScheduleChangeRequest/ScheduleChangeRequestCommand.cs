namespace Application.Features.ChangeRequests.Commands.ScheduleChangeRequest;

public record ScheduleChangeRequestCommand(Guid Id, DateTime ScheduledAt, Guid? PredecessorId = null) : ICommandRequest;
internal class ScheduleChangeRequestCommandHandler(IApplicationDbContext dbContext, BusinessMakerFramework.Shared.Abstractions.UserManagement.IUserInfoService actor) : CommandRequestHandler<ScheduleChangeRequestCommand>
{
    public override async Task<Result> Handle(ScheduleChangeRequestCommand request, CancellationToken cancellationToken)
    {
        var changeRequest = await dbContext.ChangeRequests.FindAsync([request.Id], cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        await ConfigurationChanges.EnsureOwnerAsync(dbContext, changeRequest.ProjectId, actor.Id(), !changeRequest.ProjectId.HasValue, cancellationToken);

        if (request.PredecessorId.HasValue && request.PredecessorId != changeRequest.PredecessorId)
        {
            throw new InvalidEntityStateException("مرحله پیشین پس از تأیید قابل تغییر نیست.");
        }

        var previousId = changeRequest.PredecessorId;
        var visited = new HashSet<Guid>
        {
            changeRequest.Id
        };
        var nextTime = request.ScheduledAt;

        while (previousId.HasValue)
        {
            if (visited.Count >= 20 || !visited.Add(previousId.Value))
            {
                throw new ArgumentException("زنجیره مراحل دارای چرخه یا عمق غیرمجاز است.");
            }

            var previous = await dbContext.ChangeRequests.SingleOrDefaultAsync(r => r.Id == previousId, cancellationToken) ?? throw new KeyNotFoundException("مرحله قبلی یافت نشد.");

            if (previous.ProjectId != changeRequest.ProjectId || previous.EnvironmentId != changeRequest.EnvironmentId || previous.ScheduledAt is null || previous.ScheduledAt >= nextTime || previous.Status is not (ChangeRequestStatus.Scheduled or ChangeRequestStatus.Applied))
            {
                throw new ArgumentException("مرحله قبلی باید در همین پروژه و محیط و پیش از مرحله فعلی زمان‌بندی شده باشد.");
            }

            nextTime = previous.ScheduledAt.Value;
            previousId = previous.PredecessorId;
        }

        changeRequest.SetScheduleActor(actor.Id(), changeRequest.PredecessorId);
        changeRequest.Schedule(request.ScheduledAt);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok();
    }
}