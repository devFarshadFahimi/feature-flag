using Application.Common.Interfaces;
using Domain.Aggregates.Features;
using Domain.Exceptions;
using Domain.Enums;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Features.Queries.GetFeatureById;

public record GetFeatureByIdQuery(Guid Id) : IQueryRequest<FeatureResponse>;

public record FeatureResponse(
    Guid Id,
    Guid ProjectId,
    string Name,
    string Type,
    string? Description,
    string Lifecycle,
    bool IsStale,
    bool ImpressionDataEnabled,
    DateTime CreatedAt,
    DateTime? ArchivedAt,
    List<string> Tags,
    List<FeatureEnvironmentResponse> Environments);

public record FeatureEnvironmentResponse(
    Guid Id,
    Guid EnvironmentId,
    bool Enabled,
    DateTime? LastSeenAt,
    List<FeatureStrategyResponse> Strategies);

public record FeatureStrategyResponse(
    Guid Id,
    string Type,
    int SortOrder,
    Dictionary<string, object> Parameters,
    List<ConstraintResponse> Constraints,
    List<int> SegmentIds);

public record ConstraintResponse(
    string ContextName,
    string Operator,
    List<string> Values,
    bool Inverted,
    bool CaseInsensitive);

internal class GetFeatureByIdQueryHandler(IApplicationDbContext dbContext) 
    : QueryRequestHandler<GetFeatureByIdQuery, FeatureResponse>
{
    public override async Task<FeatureResponse> Handle(GetFeatureByIdQuery request, CancellationToken cancellationToken)
{
    var feature = await dbContext.Features
        .Include(f => f.Environments)
            .ThenInclude(e => e.Strategies)
                .ThenInclude(s => s.Constraints)
        .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken)
        ?? throw new EntityNotFoundException(nameof(Feature), request.Id);

    return new FeatureResponse(
        feature.Id,
        feature.ProjectId,
        feature.Name,
        feature.Type.ToString(),
        feature.Description,
        feature.Lifecycle.ToString(),
        feature.IsStale,
        feature.ImpressionDataEnabled,
        feature.CreatedAt,
        feature.ArchivedAt,
        feature.Tags.ToList(),
        feature.Environments.Select(e => new FeatureEnvironmentResponse(
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
                s.SegmentIds.ToList())).ToList())).ToList());
}
}