using Application.Common.Interfaces;
using Application.Features.Segments.Queries.GetSegmentById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Segments.Queries.GetAllSegments;

public record GetAllSegmentsQuery : IQueryRequest<List<SegmentResponse>>;

internal class GetAllSegmentsQueryHandler(IApplicationDbContext dbContext) 
    : QueryRequestHandler<GetAllSegmentsQuery, List<SegmentResponse>>
{
    public override async Task<List<SegmentResponse>> Handle(GetAllSegmentsQuery request, CancellationToken cancellationToken)
{
    return await dbContext.Segments
        .Include(s => s.Constraints)
        .Select(s => new SegmentResponse(
            s.Id,
            s.Name,
            s.Description,
            s.IsPublic,
            s.CreatedAt,
            s.LastUsedAt,
            s.Constraints.Select(c => new SegmentConstraintResponse(
                c.ContextName,
                c.Operator.ToString(),
                c.Values.ToList(),
                c.Inverted,
                c.CaseInsensitive)).ToList()))
        .ToListAsync(cancellationToken);
}
}