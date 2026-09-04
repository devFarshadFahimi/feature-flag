namespace Application.Features.Features.Commands.ArchiveFeature;

public record ArchiveFeatureCommand(Guid Id) : ICommandRequest;

internal class ArchiveFeatureCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<ArchiveFeatureCommand>
{
    public override async Task<Result> Handle(ArchiveFeatureCommand request, CancellationToken cancellationToken)
    {
        var feature = await dbContext.Features.FindAsync([request.Id], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(Feature), request.Id + string.Empty);

        feature.Archive();
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}