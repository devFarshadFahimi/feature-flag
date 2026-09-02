using Application.Common.Interfaces;
using Application.Features.ChangeRequests.Queries.GetChangeRequestById;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ChangeRequests.Queries.GetAllChangeRequests;

public record GetAllChangeRequestsQuery(Guid? ProjectId = null, string? Status = null) : IQueryRequest<List<ChangeRequestResponse>>;

internal class GetAllChangeRequestsQueryHandler(IApplicationDbContext dbContext) 
    : QueryRequestHandler<GetAllChangeRequestsQuery, List<ChangeRequestResponse>>
{
    public override async Task<List<ChangeRequestResponse>> Handle(GetAllChangeRequestsQuery request, CancellationToken cancellationToken)
{
    var query = dbContext.ChangeRequests
        .Include(cr => cr.Items)
        .AsQueryable();

    if (request.ProjectId.HasValue)
        query = query.Where(cr => cr.ProjectId == request.ProjectId.Value);

    if (!string.IsNullOrEmpty(request.Status))
        query = query.Where(cr => cr.Status.ToString() == request.Status);

    return await query
        .Select(cr => new ChangeRequestResponse(
            cr.Id,
            cr.ProjectId,
            cr.EnvironmentId,
            cr.CreatedBy,
            cr.Status.ToString(),
            cr.Title,
            cr.Description,
            cr.ScheduledAt,
            cr.CreatedAt,
            cr.ReviewedAt,
            cr.ReviewedBy,
            cr.Items.Select(i => new ChangeRequestItemResponse(
                i.Id,
                i.Action,
                i.FeatureId,
                i.Payload)).ToList()))
        .ToListAsync(cancellationToken);
}
}