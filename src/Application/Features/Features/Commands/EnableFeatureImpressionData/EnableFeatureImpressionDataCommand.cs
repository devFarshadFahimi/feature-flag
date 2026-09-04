namespace Application.Features.Features.Commands.EnableFeatureImpressionData;

public record EnableFeatureImpressionDataCommand(Guid Id, bool Enabled) : ICommandRequest;

internal class EnableFeatureImpressionDataCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<EnableFeatureImpressionDataCommand>
{
    public override async Task<Result> Handle(EnableFeatureImpressionDataCommand request, CancellationToken cancellationToken)
    {
        var feature = await dbContext.Features.FindAsync([request.Id], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(Feature), request.Id + string.Empty);

        feature.EnableImpressionData(request.Enabled);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}