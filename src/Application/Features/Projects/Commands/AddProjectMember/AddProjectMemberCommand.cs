namespace Application.Features.Projects.Commands.AddProjectMember;

public record AddProjectMemberCommand(Guid ProjectId, Guid UserId, ProjectRole Role) : ICommandRequest;

internal class AddProjectMemberCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<AddProjectMemberCommand>
{
    public override async Task<Result> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FindAsync([request.ProjectId], cancellationToken)
            ?? throw new InvalidEntityStateException(nameof(Project), request.ProjectId + string.Empty);

        project.AddMember(request.UserId, request.Role);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}