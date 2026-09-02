
namespace Application.Features.Features.Commands.MarkFeatureAsStale;

public record MarkFeatureAsStaleCommand(Guid Id, bool IsStale) : ICommandRequest;

internal class MarkFeatureAsStaleCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<MarkFeatureAsStaleCommand>
{
    public override async Task<Result> Handle(MarkFeatureAsStaleCommand request, CancellationToken cancellationToken)
{
    var feature = await dbContext.Features.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Feature), request.Id);

    feature.MarkAsStale(request.IsStale);
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}