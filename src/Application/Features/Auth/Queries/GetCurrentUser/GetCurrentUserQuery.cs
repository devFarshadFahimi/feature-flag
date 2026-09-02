using Application.Common.Interfaces;
using Application.Features.Auth.Commands.Login;
using Domain.Aggregates.Users;
using Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IQueryRequest<UserResponse>;

internal class GetCurrentUserQueryHandler(IApplicationDbContext dbContext) 
    : QueryRequestHandler<GetCurrentUserQuery, UserResponse>
{
    public override async Task<UserResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
{
    var user = await dbContext.Users
        .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
        ?? throw new EntityNotFoundException(nameof(User), request.UserId);

    return new UserResponse(user.Id, user.Email, user.Name, user.Role.ToString());
}
}