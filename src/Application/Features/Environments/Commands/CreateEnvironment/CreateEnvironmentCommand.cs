namespace Application.Features.Environments.Commands.CreateEnvironment;

public record CreateEnvironmentCommand(string Name, EnvironmentType Type, int SortOrder = 0) : ICommandRequest<Guid>;
internal class CreateEnvironmentCommandHandler(IApplicationDbContext dbContext) : CommandRequestHandler<CreateEnvironmentCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(CreateEnvironmentCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(8610042)", cancellationToken);

        var environment = Environment.Create(request.Name, request.Type, request.SortOrder);

        _ = dbContext.Environments.Add(environment);

        var features = await dbContext.Features
            .Include(f => f.Environments)
            .Where(f => f.LifeCycle != FeatureLifecycle.Archived)
            .ToListAsync(cancellationToken);

        foreach (var feature in features)
        {
            feature.InitializeEnvironment(environment.Id);
            dbContext.FeatureEnvironments.Add(feature.GetEnvironment(environment.Id));
        }


        await dbContext.SaveChangeAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(environment.Id);
    }
}