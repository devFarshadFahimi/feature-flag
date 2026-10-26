namespace Application.Features.FeatureEnvironments.Commands.RemoveFeatureStrategy;

public record RemoveFeatureStrategyCommand(Guid FeatureId, Guid EnvironmentId, Guid StrategyId) : ICommandRequest;
internal class RemoveFeatureStrategyCommandHandler(IApplicationDbContext dbContext) : CommandRequestHandler<RemoveFeatureStrategyCommand>
{
    public override async Task<Result> Handle(RemoveFeatureStrategyCommand request, CancellationToken cancellationToken)
    {
        var featureEnv = await dbContext.FeatureEnvironments
            .Include(e => e.Strategies)
            .FirstOrDefaultAsync(fe => fe.FeatureId == request.FeatureId && fe.EnvironmentId == request.EnvironmentId, cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        featureEnv.RemoveStrategy(request.StrategyId);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok();
    }
}