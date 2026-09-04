namespace Application.Features.ChangeRequests.Commands.ScheduleChangeRequest;

public record ScheduleChangeRequestCommand(Guid Id, DateTime ScheduledAt) : ICommandRequest;

internal class ScheduleChangeRequestCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<ScheduleChangeRequestCommand>
{
    public override async Task<Result> Handle(ScheduleChangeRequestCommand request, CancellationToken cancellationToken)
    {
        var changeRequest = await dbContext.ChangeRequests.FindAsync([request.Id], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(ChangeRequest), request.Id + string.Empty);

        changeRequest.Schedule(request.ScheduledAt);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}