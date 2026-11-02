namespace Application.Features.Auth.Commands.Logout;

public record LogoutCommand(Guid UserId) : ICommandRequest;
internal class LogoutCommandHandler(IApplicationDbContext dbContext) : CommandRequestHandler<LogoutCommand>
{
    /// <summary>Serializes account-wide logout with issuance and rotation without loading token history.</summary>
    /// <param name = "request">The requested account operation.</param>
    /// <param name = "cancellationToken">Cancels database operations.</param>
    /// <returns>The successful result after persistence.</returns>
    public override async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"App\".\"TblUser\" WHERE \"Id\" = {request.UserId} FOR UPDATE", cancellationToken);

        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        user.RevokeAllRefreshTokens();
        await dbContext.Users
            .Where(u => u.Id == request.UserId)
            .SelectMany(u => u.RefreshTokens)
            .Where(t => !t.IsRevoked)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.IsRevoked, true), cancellationToken);

        await dbContext.SaveChangeAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok();
    }
}