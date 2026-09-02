using Application.Common.Interfaces;
using Domain.Aggregates.Segments;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Segments.Queries.GetSegmentById;

public record GetSegmentByIdQuery(int Id) : IQueryRequest<SegmentResponse>;

public record SegmentResponse(
    int Id,
    string Name,
    string? Description,
    bool IsPublic,
    DateTime CreatedAt,
    DateTime? LastUsedAt,
    List<SegmentConstraintResponse> Constraints);

public record SegmentConstraintResponse(
    string ContextName,
    string Operator,
    List<string> Values,
    bool Inverted,
    bool CaseInsensitive);

internal class GetSegmentByIdQueryHandler(IApplicationDbContext dbContext) 
    : QueryRequestHandler<GetSegmentByIdQuery, SegmentResponse>
{
    public override async Task<SegmentResponse> Handle(GetSegmentByIdQuery request, CancellationToken cancellationToken)
{
    var segment = await dbContext.Segments
        .Include(s => s.Constraints)
        .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
        ?? throw new EntityNotFoundException(nameof(Segment), request.Id);

    return new SegmentResponse(
        segment.Id,
        segment.Name,
        segment.Description,
        segment.IsPublic,
        segment.CreatedAt,
        segment.LastUsedAt,
        segment.Constraints.Select(c => new SegmentConstraintResponse(
            c.ContextName,
            c.Operator.ToString(),
            c.Values.ToList(),
            c.Inverted,
            c.CaseInsensitive)).ToList());
}
}