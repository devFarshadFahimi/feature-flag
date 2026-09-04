using Application.Features.Segments.Queries.GetSegmentById;

namespace Application.Features.Segments.Queries.GetAllSegments;

public record GetAllSegmentsQuery : IQueryRequest<List<SegmentResponse>>;

internal class GetAllSegmentsQueryHandler(IApplicationDbContext dbContext)
    : QueryRequestHandler<GetAllSegmentsQuery, List<SegmentResponse>>
{
    public override async Task<List<SegmentResponse>> Handle(GetAllSegmentsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Segments
            .Include(s => s.Constraints)
            .ProjectToType<SegmentResponse>()
            .ToListAsync(cancellationToken);
    }
}