using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.ChangeRequests;

namespace Application.Features.ChangeRequests.Commands.CreateChangeRequest;

public record CreateChangeRequestCommand(
    Guid ProjectId,
    Guid EnvironmentId,
    Guid CreatedBy,
    string? Title = null,
    string? Description = null) : ICommandRequest<Guid>;

internal class CreateChangeRequestCommandHandler(IApplicationDbContext dbContext, IApplicationProvider applicationProvider) 
    : CommandRequestHandler<CreateChangeRequestCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(CreateChangeRequestCommand request, CancellationToken cancellationToken)
{
    var changeRequest = ChangeRequest.Create(
        request.ProjectId,
        request.EnvironmentId,
        request.CreatedBy,
        request.Title,
        request.Description);

    dbContext.ChangeRequests.Add(changeRequest);
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok(changeRequest.Id);
}
}