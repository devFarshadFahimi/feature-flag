namespace Application.Features.Environments.Commands.CreateEnvironment;

public record CreateEnvironmentCommand(string Name, EnvironmentType Type, int SortOrder = 0) : ICommandRequest<Guid>;

internal class CreateEnvironmentCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<CreateEnvironmentCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(CreateEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var environment = Environment.Create(request.Name, request.Type, request.SortOrder);
        _ = dbContext.Environments.Add(environment);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok(environment.Id);
    }
}