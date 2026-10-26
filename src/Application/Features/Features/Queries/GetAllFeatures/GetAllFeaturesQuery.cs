using Application.Features.Features.Queries.GetFeatureById;

namespace Application.Features.Features.Queries.GetAllFeatures;

public record GetAllFeaturesQuery(Guid? ProjectId = null) : IQueryRequest<List<FeatureResponse>>;
internal class GetAllFeaturesQueryHandler(IApplicationDbContext dbContext, BusinessMakerFramework.Shared.Abstractions.UserManagement.IUserInfoService actor) : QueryRequestHandler<GetAllFeaturesQuery, List<FeatureResponse>>
{
    public override async Task<List<FeatureResponse>> Handle(GetAllFeaturesQuery request, CancellationToken cancellationToken)
    {
        var visibleProjectIds = dbContext
            .VisibleProjects(actor)
            .Select(p => p.Id);
        IQueryable<Feature>? query = dbContext.Features
            .Where(f => visibleProjectIds.Contains(f.ProjectId))
            .Include(f => f.Environments)
            .ThenInclude(e => e.Strategies)
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