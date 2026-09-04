namespace Application.Features.Projects.Commands.Update;

public record UpdateProjectCommand(Guid Id, string Name, string Description) : ICommandRequest;

internal class UpdateProjectCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<UpdateProjectCommand>
{
    public override async Task<Result> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FindAsync([request.Id], cancellationToken)
            ?? throw new InvalidEntityStateException(nameof(Project), request.Id + string.Empty);

        project.Update(request.Name, request.Description);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}