using Application.Common.DTOs;
using BusinessMakerFramework.Shared.Abstractions.UserManagement;

namespace Application.Features.Features.Queries.GetFeaturesSelective;

public record GetFeaturesSelectiveQuery(Guid? ProjectId = null) : IQueryRequest<List<SelectiveDTO<Guid>>>;

internal class GetFeaturesSelectiveQueryHandler(IApplicationDbContext dbContext, IUserInfoService actor)
    : QueryRequestHandler<GetFeaturesSelectiveQuery, List<SelectiveDTO<Guid>>>
{
    /// <summary>Scopes dropdown options to visible projects before applying the optional project filter.</summary>
    /// <param name="request">The optional project selection.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>Visible feature IDs and names ordered by name and ID.</returns>
    public override async Task<List<SelectiveDTO<Guid>>> Handle(GetFeaturesSelectiveQuery request, CancellationToken cancellationToken)
    {
        var visibleProjectIds = dbContext
            .VisibleProjects(actor)
            .Select(project => project.Id);

        var query = dbContext.Features
            .AsNoTracking()
            .Where(feature => visibleProjectIds.Contains(feature.ProjectId));

        if (request.ProjectId.HasValue)
        {
            query = query.Where(feature => feature.ProjectId == request.ProjectId.Value);
        }

        return await query
            .OrderBy(feature => feature.Name)
            .ThenBy(feature => feature.Id)
            .Select(feature => new SelectiveDTO<Guid>(feature.Id, feature.Name))
            .ToListAsync(cancellationToken);
    }
}
