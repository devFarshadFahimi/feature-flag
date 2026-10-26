namespace Application.Features.Features.Commands.UpdateFeature;

public record UpdateFeatureCommand(Guid Id, string? Description, FeatureType? Type) : ICommandRequest;
internal class UpdateFeatureCommandHandler(IApplicationDbContext dbContext) : CommandRequestHandler<UpdateFeatureCommand>
{
    public override async Task<Result> Handle(UpdateFeatureCommand request, CancellationToken cancellationToken)
    {
        var feature = await dbContext.Features.FindAsync([request.Id], cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        feature.Update(request.Description, request.Type);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok();
    }
}