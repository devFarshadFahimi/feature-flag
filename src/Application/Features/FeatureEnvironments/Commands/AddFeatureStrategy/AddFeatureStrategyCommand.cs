using Domain.Aggregates.FeatureAggregate;
using Domain.ValueObjects;

namespace Application.Features.FeatureEnvironments.Commands.AddFeatureStrategy;

public record AddFeatureStrategyCommand(Guid FeatureId, Guid EnvironmentId, StrategyType Type, int? RolloutPercentage = null, string? Stickiness = null, string? GroupId = null, List<string>? UserIds = null, List<string>? IpAddresses = null, List<string>? ApplicationNames = null) : ICommandRequest<Guid>;
internal class AddFeatureStrategyCommandHandler(IApplicationDbContext dbContext) : CommandRequestHandler<AddFeatureStrategyCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(AddFeatureStrategyCommand request, CancellationToken cancellationToken)
    {
        var environment = await FeatureConfiguration.LoadAsync(dbContext, request.FeatureId, request.EnvironmentId, cancellationToken);
        var id = await FeatureConfiguration.ConfigureAsync(dbContext, environment, new ConfigureStrategyCommand(request.FeatureId, request.EnvironmentId, null, environment.Revision, request.Type, RolloutPercentage: request.RolloutPercentage, Stickiness: request.Stickiness, GroupId: request.GroupId, UserIds: request.UserIds, IpAddresses: request.IpAddresses, ApplicationNames: request.ApplicationNames), cancellationToken);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok(id);
    }
}