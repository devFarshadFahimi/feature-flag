namespace Application.Features.Projects.Queries.GetProjectById;

public record GetProjectByIdQuery(Guid Id) : IQueryRequest<ProjectResponse>;

public record ProjectResponse(
    Guid Id,
    string Name,
    string Description,
    string DefaultStickiness,
    bool FeatureLimitEnabled,
    int? FeatureLimit,
    DateTime CreatedAt,
    List<ProjectMemberResponse> Members);

public record ProjectMemberResponse(Guid UserId, string Role);

internal class GetProjectByIdQueryHandler(IApplicationDbContext dbContext)
    : QueryRequestHandler<GetProjectByIdQuery, ProjectResponse>
{
    public override async Task<ProjectResponse> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .Include(p => p.Members)
            .Where(p => p.Id == request.Id)
            .ProjectToType<ProjectResponse>()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidEntityStateException(nameof(Project), request.Id + string.Empty);

        return project;
    }
}