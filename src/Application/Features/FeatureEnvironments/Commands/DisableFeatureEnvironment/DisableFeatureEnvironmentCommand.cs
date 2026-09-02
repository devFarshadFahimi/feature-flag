using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Features;
using Domain.Exceptions;

namespace Application.Features.FeatureEnvironments.Commands.DisableFeatureEnvironment;

public record DisableFeatureEnvironmentCommand(Guid FeatureId, Guid EnvironmentId) : ICommandRequest;

internal class DisableFeatureEnvironmentCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<DisableFeatureEnvironmentCommand>
{
    public override async Task<Result> Handle(DisableFeatureEnvironmentCommand request, CancellationToken cancellationToken)
{
    var featureEnv = await dbContext.FeatureEnvironments
        .FirstOrDefaultAsync(fe => fe.FeatureId == request.FeatureId && fe.EnvironmentId == request.EnvironmentId, cancellationToken)
        ?? throw new EntityNotFoundException(nameof(FeatureEnvironment), $"{request.FeatureId}-{request.EnvironmentId}");

    featureEnv.Disable();
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}