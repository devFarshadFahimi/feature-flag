using Application.Common.Interfaces;
using Application.Features.Users.Queries.GetUserById;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users.Queries.GetAllUsers;

public record GetAllUsersQuery : IQueryRequest<List<UserResponse>>;

internal class GetAllUsersQueryHandler(IApplicationDbContext dbContext) 
    : QueryRequestHandler<GetAllUsersQuery, List<UserResponse>>
{
    public override async Task<List<UserResponse>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
{
    return await dbContext.Users
        .Select(u => new UserResponse(
            u.Id,
            u.Email,
            u.Name,
            u.Role.ToString(),
            u.IsActive,
            u.CreatedAt,
            u.LastLoginAt))
        .ToListAsync(cancellationToken);
}
}