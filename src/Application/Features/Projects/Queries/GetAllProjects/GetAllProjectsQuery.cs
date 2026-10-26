using Application.Features.Projects.Queries.GetProjectById;

namespace Application.Features.Projects.Queries.GetAllProjects;

public record GetAllProjectsQuery : IQueryRequest<List<ProjectResponse>>;
internal class GetAllProjectsQueryHandler(IApplicationDbContext dbContext, BusinessMakerFramework.Shared.Abstractions.UserManagement.IUserInfoService actor) : QueryRequestHandler<GetAllProjectsQuery, List<ProjectResponse>>
{
    public override async Task<List<ProjectResponse>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext
            .VisibleProjects(actor)
            .Include(p => p.Members)
            .ProjectToType<ProjectResponse>()
            .ToListAsync(cancellationToken);
    }
}