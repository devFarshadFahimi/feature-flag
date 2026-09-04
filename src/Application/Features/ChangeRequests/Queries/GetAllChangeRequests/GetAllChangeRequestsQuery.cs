using Application.Features.ChangeRequests.Queries.GetChangeRequestById;

namespace Application.Features.ChangeRequests.Queries.GetAllChangeRequests;

public record GetAllChangeRequestsQuery(Guid? ProjectId = null, string? Status = null) : IQueryRequest<List<ChangeRequestResponse>>;

internal class GetAllChangeRequestsQueryHandler(IApplicationDbContext dbContext)
    : QueryRequestHandler<GetAllChangeRequestsQuery, List<ChangeRequestResponse>>
{
    public override async Task<List<ChangeRequestResponse>> Handle(GetAllChangeRequestsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ChangeRequest>? query = dbContext.ChangeRequests
            .Include(cr => cr.Items)
            .AsQueryable();

        if (request.ProjectId.HasValue)
        {
            query = query.Where(cr => cr.ProjectId == request.ProjectId.Value);
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(cr => cr.Status.ToString() == request.Status);
        }

        return await query
            .ProjectToType<ChangeRequestResponse>()
            .ToListAsync(cancellationToken);
    }
}