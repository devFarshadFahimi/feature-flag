namespace Application.Features.Features.Commands.RemoveFeatureTag;

public record RemoveFeatureTagCommand(Guid FeatureId, string Tag) : ICommandRequest;

internal class RemoveFeatureTagCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<RemoveFeatureTagCommand>
{
    public override async Task<Result> Handle(RemoveFeatureTagCommand request, CancellationToken cancellationToken)
    {
        var feature = await dbContext.Features.FindAsync([request.FeatureId], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(Feature), request.FeatureId + string.Empty);

        feature.RemoveTag(request.Tag);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}