using Application.Features.Auth.Commands.Login;
using Domain.Aggregates.UserAggregate;

namespace Application.Features.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IQueryRequest<UserResponse>;

internal class GetCurrentUserQueryHandler(IApplicationDbContext dbContext)
    : QueryRequestHandler<GetCurrentUserQuery, UserResponse>
{
    public override async Task<UserResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new InvalidEntityStateException(nameof(User), request.UserId + string.Empty);

        return new UserResponse(user.Id, user.Email, user.Name, user.Role.ToString());
    }
}