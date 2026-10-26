using Application.Features.ChangeRequests.Queries.GetChangeRequestById;

namespace Application.Features.ChangeRequests.Queries.GetAllChangeRequests;

public record GetAllChangeRequestsQuery(Guid? ProjectId = null, string? Status = null) : IQueryRequest<List<ChangeRequestResponse>>;
internal class GetAllChangeRequestsQueryHandler(IApplicationDbContext dbContext, BusinessMakerFramework.Shared.Abstractions.UserManagement.IUserInfoService actor) : QueryRequestHandler<GetAllChangeRequestsQuery, List<ChangeRequestResponse>>
{
    public override async Task<List<ChangeRequestResponse>> Handle(GetAllChangeRequestsQuery request, CancellationToken cancellationToken)
    {
        var visibleProjectIds = dbContext
            .VisibleProjects(actor)
            .Select(p => p.Id);
        IQueryable<ChangeRequest>? query = dbContext.ChangeRequests
            .Where(r => (r.ProjectId == null ? dbContext.Users.Any(u => u.Id == actor.Id() && u.IsActive && u.Role >= UserRole.Admin) : visibleProjectIds.Contains(r.ProjectId!.Value)))
            .Include(cr => cr.Items)
            .AsQueryable();

        if (request.ProjectId.HasValue)
        {
            query = query.Where(cr => cr.ProjectId == request.ProjectId.Value);
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            if (!Enum.TryParse<ChangeRequestStatus>(request.Status, true, out var status) || !Enum.IsDefined(status))
            {
                throw new ArgumentException("وضعیت درخواست تغییر معتبر نیست.");
            }


            query = query.Where(cr => cr.Status == status);
        }


        return await query
            .ProjectToType<ChangeRequestResponse>()
            .ToListAsync(cancellationToken);
    }
}