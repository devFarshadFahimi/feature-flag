using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Features;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Application.Features.FeatureEnvironments.Commands.AddFeatureStrategy;

public record AddFeatureStrategyCommand(
    Guid FeatureId,
    Guid EnvironmentId,
    StrategyType Type,
    int? RolloutPercentage = null,
    string? Stickiness = null,
    string? GroupId = null,
    List<string>? UserIds = null,
    List<string>? IpAddresses = null,
    List<string>? ApplicationNames = null) : ICommandRequest<Guid>;

internal class AddFeatureStrategyCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<AddFeatureStrategyCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(AddFeatureStrategyCommand request, CancellationToken cancellationToken)
{
    var featureEnv = await dbContext.FeatureEnvironments
        .FirstOrDefaultAsync(fe => fe.FeatureId == request.FeatureId && fe.EnvironmentId == request.EnvironmentId, cancellationToken)
        ?? throw new EntityNotFoundException(nameof(FeatureEnvironment), $"{request.FeatureId}-{request.EnvironmentId}");

    var parameters = request.Type switch
    {
        StrategyType.FlexibleRollout => StrategyParameters.CreateGradualRollout(request.RolloutPercentage ?? 100, request.Stickiness ?? "default", request.GroupId),
        StrategyType.UserWithId => StrategyParameters.CreateUserIds(request.UserIds ?? new()),
        StrategyType.RemoteAddress => StrategyParameters.CreateRemoteAddress(request.IpAddresses ?? new()),
        StrategyType.ApplicationHost => StrategyParameters.CreateApplicationHost(request.ApplicationNames ?? new()),
        _ => StrategyParameters.CreateDefault()
    };

    var strategy = featureEnv.AddStrategy(request.Type, parameters);
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok(strategy.Id);
}
}