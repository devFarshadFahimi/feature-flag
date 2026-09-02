using Application.Common.Interfaces;
using Application.Features.Features.Queries.GetFeatureById;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Features.Queries.GetAllFeatures;

public record GetAllFeaturesQuery(Guid? ProjectId = null) : IQueryRequest<List<FeatureResponse>>;

internal class GetAllFeaturesQueryHandler(IApplicationDbContext dbContext) 
    : QueryRequestHandler<GetAllFeaturesQuery, List<FeatureResponse>>
{
    public override async Task<List<FeatureResponse>> Handle(GetAllFeaturesQuery request, CancellationToken cancellationToken)
{
    var query = dbContext.Features
        .Include(f => f.Environments)
            .ThenInclude(e => e.Strategies)
                .ThenInclude(s => s.Constraints)
        .AsQueryable();

    if (request.ProjectId.HasValue)
        query = query.Where(f => f.ProjectId == request.ProjectId.Value);

    return await query
        .Select(f => new FeatureResponse(
            f.Id,
            f.ProjectId,
            f.Name,
            f.Type.ToString(),
            f.Description,
            f.Lifecycle.ToString(),
            f.IsStale,
            f.ImpressionDataEnabled,
            f.CreatedAt,
            f.ArchivedAt,
            f.Tags.ToList(),
            f.Environments.Select(e => new FeatureEnvironmentResponse(
                e.Id,
                e.EnvironmentId,
                e.Enabled,
                e.LastSeenAt,
                e.Strategies.Select(s => new FeatureStrategyResponse(
                    s.Id,
                    s.Type.ToString(),
                    s.SortOrder,
                    new Dictionary<string, object>
                    {
                        ["rolloutPercentage"] = s.Parameters.RolloutPercentage ?? 0,
                        ["stickiness"] = s.Parameters.Stickiness ?? "default",
                        ["groupId"] = s.Parameters.GroupId ?? "",
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
                    s.SegmentIds.ToList())).ToList())).ToList()))
        .ToListAsync(cancellationToken);
}
}