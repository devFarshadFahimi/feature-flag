namespace Application.Features.Features.Commands.RemoveFeatureTag;

public record RemoveFeatureTagCommand(Guid FeatureId, string Tag) : ICommandRequest;
internal class RemoveFeatureTagCommandHandler(IApplicationDbContext dbContext) : CommandRequestHandler<RemoveFeatureTagCommand>
{
    public override async Task<Result> Handle(RemoveFeatureTagCommand request, CancellationToken cancellationToken)
    {
        var feature = await dbContext.Features.FindAsync([request.FeatureId], cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        feature.RemoveTag(request.Tag);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok();
    }
}