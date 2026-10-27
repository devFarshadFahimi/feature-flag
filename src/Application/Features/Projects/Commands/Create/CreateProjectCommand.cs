namespace Application.Features.Projects.Commands.Create;

public record CreateProjectCommand(string Name, string Description, string DefaultStickiness = Domain.ValueObjects.StrategyParameters.DefaultStickiness) : ICommandRequest<Guid>;
internal class CreateProjectCommandHandler(IApplicationDbContext dbContext, BusinessMakerFramework.Shared.Abstractions.UserManagement.IUserInfoService actor) : CommandRequestHandler<CreateProjectCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = Project.Create(request.Name, request.Description, request.DefaultStickiness);

        project.AddMember(actor.Id(), ProjectRole.Owner);
        dbContext.Projects.Add(project);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok(project.Id);
    }
}