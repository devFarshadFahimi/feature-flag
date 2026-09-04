namespace Application.Features.FeatureEnvironments.Commands.RemoveFeatureStrategy;

public record RemoveFeatureStrategyCommand(Guid FeatureId, Guid EnvironmentId, Guid StrategyId) : ICommandRequest;

internal class RemoveFeatureStrategyCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<RemoveFeatureStrategyCommand>
{
    public override async Task<Result> Handle(RemoveFeatureStrategyCommand request, CancellationToken cancellationToken)
    {
        var featureEnv = await dbContext.FeatureEnvironments
            .FirstOrDefaultAsync(fe => fe.FeatureId == request.FeatureId && fe.EnvironmentId == request.EnvironmentId, cancellationToken)
            ?? throw new InvalidEntityStateException(nameof(FeatureEnvironment), $"{request.FeatureId}-{request.EnvironmentId}");

        featureEnv.RemoveStrategy(request.StrategyId);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}