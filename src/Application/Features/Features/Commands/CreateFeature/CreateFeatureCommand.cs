using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Features;
using Domain.Aggregates.Projects;
using Domain.Enums;
using Domain.Exceptions;
namespace Application.Features.Features.Commands.CreateFeature;

public record CreateFeatureCommand(Guid ProjectId, string Name, FeatureType Type, string? Description = null) : ICommandRequest<Guid>;

internal class CreateFeatureCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<CreateFeatureCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(CreateFeatureCommand request, CancellationToken cancellationToken)
{
    var project = await dbContext.Projects.FindAsync([request.ProjectId], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), request.ProjectId);

    var feature = project.AddFeature(request.Name, request.Type, request.Description);
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok(feature.Id);
}
}