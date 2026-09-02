
namespace Application.Features.Features.Commands.ArchiveFeature;

public record ArchiveFeatureCommand(Guid Id) : ICommandRequest;

internal class ArchiveFeatureCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<ArchiveFeatureCommand>
{
    public override async Task<Result> Handle(ArchiveFeatureCommand request, CancellationToken cancellationToken)
{
    var feature = await dbContext.Features.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Feature), request.Id);

    feature.Archive();
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}