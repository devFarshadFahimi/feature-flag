using Application.Features.Features.Queries.GetFeatureById;

namespace Application.Features.FeatureEnvironments.Queries.GetFeatureEnvironmentById;

public record GetFeatureEnvironmentByIdQuery(Guid FeatureId, Guid EnvironmentId) : IQueryRequest<FeatureEnvironmentResponse>;

internal class GetFeatureEnvironmentByIdQueryHandler(IApplicationDbContext dbContext)
    : QueryRequestHandler<GetFeatureEnvironmentByIdQuery, FeatureEnvironmentResponse>
{
    public override async Task<FeatureEnvironmentResponse> Handle(GetFeatureEnvironmentByIdQuery request, CancellationToken cancellationToken)
    {
        var featureEnv = await dbContext.FeatureEnvironments
            .Include(fe => fe.Strategies)
                .ThenInclude(s => s.Constraints)
            .FirstOrDefaultAsync(fe => fe.FeatureId == request.FeatureId && fe.EnvironmentId == request.EnvironmentId, cancellationToken)
            ?? throw new InvalidEntityStateException(nameof(FeatureEnvironment), $"{request.FeatureId}-{request.EnvironmentId}");

        return new FeatureEnvironmentResponse(
            featureEnv.Id,
            featureEnv.EnvironmentId,
            featureEnv.Enabled,
            featureEnv.LastSeenAt,
            featureEnv.Strategies.Select(s => new FeatureStrategyResponse(
                s.Id,
                s.Type.ToString(),
                s.SortOrder,
                new Dictionary<string, object>
                {
                    ["rolloutPercentage"] = s.Parameters.RolloutPercentage ?? 0,
                    ["stickiness"] = s.Parameters.Stickiness ?? "default",
                    ["groupId"] = s.Parameters.GroupId ?? string.Empty,
                    ["userIds"] = s.Parameters.UserIds,
                    ["ipAddresses"] = s.Parameters.IpAddresses,
                    ["applicationNames"] = s.Parameters.ApplicationNames
                },
                s.Constraints.Select(c => new ConstraintResponse(
                    c.ContextName,
                    c.Operator.ToString(),
                    c.Values.ToList(),
                    c.Inverted,
                    c.CaseInsensitive)).ToList(),
                s.SegmentIds.ToList())).ToList());
    }
}