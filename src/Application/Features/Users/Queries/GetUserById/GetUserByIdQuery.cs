namespace Application.Features.Users.Queries.GetUserById;

public record GetUserByIdQuery(Guid Id) : IQueryRequest<UserResponse>;

public record UserResponse(
    Guid Id,
    string Email,
    string? Name,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

internal class GetUserByIdQueryHandler(IApplicationDbContext dbContext)
    : QueryRequestHandler<GetUserByIdQuery, UserResponse>
{
    public override async Task<UserResponse> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Where(u => u.Id == request.Id)
            .ProjectToType<UserResponse>()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidEntityStateException(nameof(User), request.Id + string.Empty);

        return user;
    }
}