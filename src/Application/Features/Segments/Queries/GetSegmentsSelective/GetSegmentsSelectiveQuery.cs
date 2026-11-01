using Application.Common.DTOs;
using BusinessMakerFramework.Shared.Abstractions.UserManagement;

namespace Application.Features.Segments.Queries.GetSegmentsSelective;

public record GetSegmentsSelectiveQuery(Guid? ProjectId = null, bool IncludeShared = true) : IQueryRequest<List<SelectiveDTO<int>>>;
internal sealed class GetSegmentsSelectiveHandler(IApplicationDbContext db, IUserInfoService actor) : QueryRequestHandler<GetSegmentsSelectiveQuery, List<SelectiveDTO<int>>>
{
    /// <summary>Returns only visible project segments and optional shared segments, without exposing constraints.</summary>
    /// <param name="request">The request containing the operation's input and scope.</param>
    /// <param name="ct">Cancels the operation and its database or network requests.</param>
    /// <returns>The operation result, including the requested response data when applicable.</returns>
    public override async Task<List<SelectiveDTO<int>>> Handle(GetSegmentsSelectiveQuery request, CancellationToken ct)
    {
        var visibleIds = db
            .VisibleProjects(actor)
            .Select(project => project.Id);

        return await db.Segments
            .AsNoTracking()
            .Where(segment => segment.ProjectId == null ? request.IncludeShared : visibleIds.Contains(segment.ProjectId.Value) && (!request.ProjectId.HasValue || segment.ProjectId == request.ProjectId))
            .OrderBy(segment => segment.Name)
            .ThenBy(segment => segment.Id)
            .Select(segment => new SelectiveDTO<int>(segment.Id, segment.Name))
            .ToListAsync(ct);
    }
}