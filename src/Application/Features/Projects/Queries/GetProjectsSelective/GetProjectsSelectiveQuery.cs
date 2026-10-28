using Application.Common.DTOs;
using BusinessMakerFramework.Shared.Abstractions.UserManagement;

namespace Application.Features.Projects.Queries.GetProjectsSelective;

public record GetProjectsSelectiveQuery : IQueryRequest<List<SelectiveDTO<Guid>>>;

internal class GetProjectsSelectiveQueryHandler(IApplicationDbContext dbContext, IUserInfoService actor)
    : QueryRequestHandler<GetProjectsSelectiveQuery, List<SelectiveDTO<Guid>>>
{
    /// <summary>Projects dropdown options using the same current-account and membership scope as project reads.</summary>
    /// <param name="request">The dropdown query.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>Visible project IDs and names, ordered by name and ID.</returns>
    public override async Task<List<SelectiveDTO<Guid>>> Handle(GetProjectsSelectiveQuery request, CancellationToken cancellationToken)
    {
        return await dbContext
            .VisibleProjects(actor)
            .AsNoTracking()
            .OrderBy(project => project.Name)
            .ThenBy(project => project.Id)
            .Select(project => new SelectiveDTO<Guid>(project.Id, project.Name))
            .ToListAsync(cancellationToken);
    }
}
