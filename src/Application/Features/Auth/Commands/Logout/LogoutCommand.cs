using Domain.Aggregates.UserAggregate;

namespace Application.Features.Auth.Commands.Logout;

public record LogoutCommand(Guid UserId) : ICommandRequest;

internal class LogoutCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<LogoutCommand>
{
    public override async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new InvalidEntityStateException(nameof(User), request.UserId + string.Empty);

        user.RevokeAllRefreshTokens();
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}