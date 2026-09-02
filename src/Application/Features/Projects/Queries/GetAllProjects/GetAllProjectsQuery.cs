using Application.Common.Interfaces;
using Application.Features.Projects.Queries.GetProjectById;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Projects.Queries.GetAllProjects;

public record GetAllProjectsQuery : IQueryRequest<List<ProjectResponse>>;

internal class GetAllProjectsQueryHandler(IApplicationDbContext dbContext) 
    : QueryRequestHandler<GetAllProjectsQuery, List<ProjectResponse>>
{
    public override async Task<List<ProjectResponse>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
{
    return await dbContext.Projects
        .Include(p => p.Members)
        .Select(p => new ProjectResponse(
            p.Id,
            p.Name,
            p.Description,
            p.DefaultStickiness,
            p.FeatureLimitEnabled,
            p.FeatureLimit,
            p.CreatedAt,
            p.Members.Select(m => new ProjectMemberResponse(m.UserId, m.Role.ToString())).ToList()))
        .ToListAsync(cancellationToken);
}
}