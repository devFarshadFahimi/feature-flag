namespace Application.Features.Features.Queries.GetFeatureById;

public record GetFeatureByIdQuery(Guid Id) : IQueryRequest<FeatureResponse>;
public record FeatureResponse(Guid Id, Guid ProjectId, string Name, string Type, string? Description, string Lifecycle, bool IsStale, bool ImpressionDataEnabled, DateTime CreatedAt, DateTime? ArchivedAt, List<string> Tags, List<FeatureEnvironmentResponse> Environments, long Revision = 0, List<Domain.Evaluation.Prerequisite>? Prerequisites = null);
public record FeatureEnvironmentResponse(Guid Id, Guid EnvironmentId, bool Enabled, DateTime? LastSeenAt, List<FeatureStrategyResponse> Strategies, long Revision = 0, List<Domain.ValueObjects.Variant>? Variants = null);
public record FeatureStrategyResponse(Guid Id, string Type, int SortOrder, Dictionary<string, object> Parameters, List<ConstraintResponse> Constraints, List<int> SegmentIds, long Revision = 0, bool Enabled = true, List<Domain.ValueObjects.Variant>? Variants = null);
public record ConstraintResponse(string ContextName, string Operator, List<string> Values, bool Inverted, bool CaseInsensitive);
internal class GetFeatureByIdQueryHandler(IApplicationDbContext dbContext) : QueryRequestHandler<GetFeatureByIdQuery, FeatureResponse>
{
    public override async Task<FeatureResponse> Handle(GetFeatureByIdQuery request, CancellationToken cancellationToken)
    {
        var feature = await dbContext.Features
            .Where(f => f.Id == request.Id)
            .Include(f => f.Environments)
            .ThenInclude(e => e.Strategies)
            .ThenInclude(s => s.Constraints)
            .AsSplitQuery()
            .ProjectToType<FeatureResponse>()
            .FirstOrDefaultAsync(cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        return feature;
    }
}