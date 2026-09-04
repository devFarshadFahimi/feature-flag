namespace Application.Features.Projects.Commands.RemoveProjectMember;

public record RemoveProjectMemberCommand(Guid ProjectId, Guid UserId) : ICommandRequest;

internal class RemoveProjectMemberCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<RemoveProjectMemberCommand>
{
    public override async Task<Result> Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FindAsync([request.ProjectId], cancellationToken)
            ?? throw new InvalidEntityStateException(nameof(Project), request.ProjectId + string.Empty);

        project.RemoveMember(request.UserId);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}