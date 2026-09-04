namespace Application.Features.Projects.Commands.Create;


public record CreateProjectCommand(string Name, string Description, string DefaultStickiness = "default") : ICommandRequest<Guid>;

internal class CreateProjectCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<CreateProjectCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = Project.Create(request.Name, request.Description, request.DefaultStickiness);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok(project.Id);
    }
}