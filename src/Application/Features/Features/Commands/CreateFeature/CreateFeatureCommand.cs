namespace Application.Features.Features.Commands.CreateFeature;

public record CreateFeatureCommand(Guid ProjectId, string Name, FeatureType Type, string? Description = null) : ICommandRequest<Guid>;

internal class CreateFeatureCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<CreateFeatureCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(CreateFeatureCommand request, CancellationToken cancellationToken)
    {
        Project? project = await dbContext.Projects.FindAsync([request.ProjectId], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(Project), request.ProjectId + string.Empty);

        Feature? feature = Feature.Create(project.Id, request.Name, request.Type, request.Description);

        _ = await dbContext.Features.AddAsync(feature, cancellationToken);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok(feature.Id);
    }
}