namespace Application.Features.FeatureEnvironments.Commands.DisableFeatureEnvironment;

public record DisableFeatureEnvironmentCommand(Guid FeatureId, Guid EnvironmentId) : ICommandRequest;
internal class DisableFeatureEnvironmentCommandHandler(IApplicationDbContext dbContext) : CommandRequestHandler<DisableFeatureEnvironmentCommand>
{
    public override async Task<Result> Handle(DisableFeatureEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var featureEnv = await dbContext.FeatureEnvironments.FirstOrDefaultAsync(fe => fe.FeatureId == request.FeatureId && fe.EnvironmentId == request.EnvironmentId, cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        featureEnv.Disable();

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok();
    }
}