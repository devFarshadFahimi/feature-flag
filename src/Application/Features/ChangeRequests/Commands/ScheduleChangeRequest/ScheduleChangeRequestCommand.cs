using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.ChangeRequests;
using Domain.Exceptions;

namespace Application.Features.ChangeRequests.Commands.ScheduleChangeRequest;

public record ScheduleChangeRequestCommand(Guid Id, DateTime ScheduledAt) : ICommandRequest;

internal class ScheduleChangeRequestCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<ScheduleChangeRequestCommand>
{
    public override async Task<Result> Handle(ScheduleChangeRequestCommand request, CancellationToken cancellationToken)
{
    var changeRequest = await dbContext.ChangeRequests.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ChangeRequest), request.Id);

    changeRequest.Schedule(request.ScheduledAt);
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}