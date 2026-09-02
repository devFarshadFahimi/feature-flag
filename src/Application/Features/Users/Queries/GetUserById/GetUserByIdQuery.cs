using Application.Common.Interfaces;
using Domain.Aggregates.Users;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

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
        .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
        ?? throw new EntityNotFoundException(nameof(User), request.Id);

    return new UserResponse(
        user.Id,
        user.Email,
        user.Name,
        user.Role.ToString(),
        user.IsActive,
        user.CreatedAt,
        user.LastLoginAt);
}
}