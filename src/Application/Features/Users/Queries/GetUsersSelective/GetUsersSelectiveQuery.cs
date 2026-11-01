using Application.Common.DTOs;

namespace Application.Features.Users.Queries.GetUsersSelective;

public record GetUsersSelectiveQuery(bool IncludeInactive = false) : IQueryRequest<List<SelectiveDTO<Guid>>>;
internal sealed class GetUsersSelectiveHandler(IApplicationDbContext db) : QueryRequestHandler<GetUsersSelectiveQuery, List<SelectiveDTO<Guid>>>
{
    public override async Task<List<SelectiveDTO<Guid>>> Handle(GetUsersSelectiveQuery request, CancellationToken ct)
    {
        return await db.Users
            .AsNoTracking()
            .Where(user => request.IncludeInactive || user.IsActive)
            .OrderBy(user => user.Name ?? user.Email)
            .ThenBy(user => user.Id)
            .Select(user => new SelectiveDTO<Guid>(user.Id, user.Name == null || user.Name == "" ? user.Email : user.Name))
            .ToListAsync(ct);
    }
}