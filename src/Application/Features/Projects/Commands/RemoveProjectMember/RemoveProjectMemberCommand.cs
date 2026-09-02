using Application.Common.Services;
using Domain.Aggregates.ProjectAggregate;

namespace Application.Features.Projects.Commands.RemoveProjectMember;

public record RemoveProjectMemberCommand(Guid ProjectId, Guid UserId) : ICommandRequest;

internal class RemoveProjectMemberCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<RemoveProjectMemberCommand>
{
    public override async Task<Result> Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FindAsync([request.ProjectId], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), request.ProjectId);

        project.RemoveMember(request.UserId);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}