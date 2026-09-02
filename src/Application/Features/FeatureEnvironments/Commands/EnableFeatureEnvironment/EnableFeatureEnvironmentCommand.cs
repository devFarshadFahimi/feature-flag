using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Features;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.FeatureEnvironments.Commands.EnableFeatureEnvironment;

public record EnableFeatureEnvironmentCommand(Guid FeatureId, Guid EnvironmentId) : ICommandRequest;

internal class EnableFeatureEnvironmentCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<EnableFeatureEnvironmentCommand>
{
    public override async Task<Result> Handle(EnableFeatureEnvironmentCommand request, CancellationToken cancellationToken)
{
    var featureEnv = await dbContext.FeatureEnvironments
        .FirstOrDefaultAsync(fe => fe.FeatureId == request.FeatureId && fe.EnvironmentId == request.EnvironmentId, cancellationToken)
        ?? throw new EntityNotFoundException(nameof(FeatureEnvironment), $"{request.FeatureId}-{request.EnvironmentId}");

    featureEnv.Enable();
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}