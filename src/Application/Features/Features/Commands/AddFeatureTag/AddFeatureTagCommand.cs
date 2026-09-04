namespace Application.Features.Features.Commands.AddFeatureTag;

public record AddFeatureTagCommand(Guid FeatureId, string Tag) : ICommandRequest;

internal class AddFeatureTagCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<AddFeatureTagCommand>
{
    public override async Task<Result> Handle(AddFeatureTagCommand request, CancellationToken cancellationToken)
    {
        var feature = await dbContext.Features.FindAsync([request.FeatureId], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(Feature), request.FeatureId + string.Empty);

        feature.AddTag(request.Tag);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}