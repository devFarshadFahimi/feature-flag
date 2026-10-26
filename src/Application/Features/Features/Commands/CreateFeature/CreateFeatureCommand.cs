namespace Application.Features.Features.Commands.CreateFeature;

public record CreateFeatureCommand(Guid ProjectId, string Name, FeatureType Type, string? Description = null) : ICommandRequest<Guid>;
internal class CreateFeatureCommandHandler(IApplicationDbContext dbContext) : CommandRequestHandler<CreateFeatureCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(CreateFeatureCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(8610042)", cancellationToken);

        Project? project = await dbContext.Projects
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken) ?? throw new KeyNotFoundException("پروژه یافت نشد.");
        Feature feature = project.AddFeature(request.Name, request.Type, request.Description);

        foreach (var environmentId in await dbContext.Environments
            .Select(e => e.Id)
            .ToListAsync(cancellationToken))
        {
            feature.InitializeEnvironment(environmentId);
        }

        _ = await dbContext.Features.AddAsync(feature, cancellationToken);

        await dbContext.SaveChangeAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(feature.Id);
    }
}