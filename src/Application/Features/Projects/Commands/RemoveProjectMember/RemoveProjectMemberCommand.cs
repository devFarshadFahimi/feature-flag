namespace Application.Features.Projects.Commands.RemoveProjectMember;

public record RemoveProjectMemberCommand(Guid ProjectId, Guid UserId) : ICommandRequest;
internal class RemoveProjectMemberCommandHandler(IApplicationDbContext dbContext) : CommandRequestHandler<RemoveProjectMemberCommand>
{
    public override async Task<Result> Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        project.RemoveMember(request.UserId);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok();
    }
}