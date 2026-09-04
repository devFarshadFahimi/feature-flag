using Application.Features.Users.Queries.GetUserById;

namespace Application.Features.Users.Queries.GetAllUsers;

public record GetAllUsersQuery : IQueryRequest<List<UserResponse>>;

internal class GetAllUsersQueryHandler(IApplicationDbContext dbContext)
    : QueryRequestHandler<GetAllUsersQuery, List<UserResponse>>
{
    public override async Task<List<UserResponse>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .ProjectToType<UserResponse>()
            .ToListAsync(cancellationToken);
    }
}