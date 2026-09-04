using Application.Features.Features.Queries.GetFeatureById;

namespace Application.Features.Features.Queries.GetAllFeatures;

public record GetAllFeaturesQuery(Guid? ProjectId = null) : IQueryRequest<List<FeatureResponse>>;

internal class GetAllFeaturesQueryHandler(IApplicationDbContext dbContext)
    : QueryRequestHandler<GetAllFeaturesQuery, List<FeatureResponse>>
{
    public override async Task<List<FeatureResponse>> Handle(GetAllFeaturesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Feature>? query = dbContext.Features
            .Include(f => f.Environments)
            .ThenInclude(e => e.Strategies)
            //    .ThenInclude(s => s.Constraints)
            .AsQueryable();

        if (request.ProjectId.HasValue)
        {
            query = query.Where(f => f.ProjectId == request.ProjectId.Value);
        }

        return await query
        .ProjectToType<FeatureResponse>()
        .ToListAsync(cancellationToken);
    }
}