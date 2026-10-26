using Application.Features.Segments.Queries.GetSegmentById;

namespace Application.Features.Segments.Queries.GetAllSegments;

public record GetAllSegmentsQuery : IQueryRequest<List<SegmentResponse>>;
internal class GetAllSegmentsQueryHandler(IApplicationDbContext dbContext, BusinessMakerFramework.Shared.Abstractions.UserManagement.IUserInfoService actor) : QueryRequestHandler<GetAllSegmentsQuery, List<SegmentResponse>>
{
    public override async Task<List<SegmentResponse>> Handle(GetAllSegmentsQuery request, CancellationToken cancellationToken)
    {
        var visibleProjectIds = dbContext
            .VisibleProjects(actor)
            .Select(p => p.Id);

        return await dbContext.Segments
            .Where(s => s.ProjectId == null || visibleProjectIds.Contains(s.ProjectId!.Value))
            .Include(s => s.Constraints)
            .ProjectToType<SegmentResponse>()
            .ToListAsync(cancellationToken);
    }
}