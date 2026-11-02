using System.Security.Claims;
using Application.Features.Auth.Commands.Login;
using TokenEntity = Domain.Aggregates.UserAggregate.RefreshToken;

namespace Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : ICommandRequest<LoginResponse>;
internal class RefreshTokenCommandHandler(IApplicationDbContext dbContext, IJwtTokenGenerator jwtTokenGenerator, IOptions<JwtSettings> jwtSettings) : CommandRequestHandler<RefreshTokenCommand, LoginResponse>
{
    /// <summary>Locks the account for single-use rotation and commits account-wide revocation before rejecting replay.</summary>
    /// <param name = "request">The requested account operation.</param>
    /// <param name = "cancellationToken">Cancels database operations.</param>
    /// <returns>The successful result after persistence.</returns>
    public override async Task<Result<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.AccessToken) || string.IsNullOrEmpty(request.RefreshToken) || request.RefreshToken.Length > 512)
        {
            throw new UnauthorizedAccessException("توکن تمدید معتبر نیست.");
        }

        var principal = jwtTokenGenerator.ValidateToken(request.AccessToken);

        if (principal is null || !Guid.TryParse((principal.FindFirst(IdentityClaimNames.Subject) ?? principal.FindFirst(ClaimTypes.NameIdentifier))?.Value, out var userId))
        {
            throw new UnauthorizedAccessException("توکن دسترسی معتبر نیست.");
        }

        var tokenHash = TokenEntity.Hash(request.RefreshToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"App\".\"TblUser\" WHERE \"Id\" = {userId} FOR UPDATE", cancellationToken);

        var user = await dbContext.Users
            .Include(u => u.RefreshTokens.Where(t => t.Token == tokenHash || (t.Token == request.RefreshToken && !t.Token.StartsWith(TokenEntity.HashPrefix))))
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken) ?? throw new UnauthorizedAccessException("توکن تمدید معتبر نیست.");
        var refreshToken = user.RefreshTokens.SingleOrDefault() ?? throw new UnauthorizedAccessException("توکن تمدید معتبر نیست.");

        if (!refreshToken.Token.StartsWith(TokenEntity.HashPrefix, StringComparison.Ordinal) || refreshToken.IsUsed)
        {
            user.RevokeAllRefreshTokens();
            await dbContext.Users
                .Where(u => u.Id == userId)
                .SelectMany(u => u.RefreshTokens)
                .Where(t => !t.IsRevoked)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.IsRevoked, true), cancellationToken);

            await dbContext.SaveChangeAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            throw new UnauthorizedAccessException("نشست به دلیل استفاده مجدد یا قدیمی بودن توکن لغو شد؛ دوباره وارد شوید.");
        }

        if (!user.IsActive || principal.FindFirst(IdentityClaimNames.SecurityVersion)?.Value != user.SecurityVersion.ToString() || !refreshToken.IsValid())
        {
            throw new UnauthorizedAccessException("توکن تمدید معتبر نیست.");
        }

        refreshToken.MarkAsUsed();

        var newAccessToken = jwtTokenGenerator.GenerateAccessToken(user);
        var newRefreshTokenValue = jwtTokenGenerator.GenerateRefreshToken();
        var settings = jwtSettings.Value;
        var newRefreshToken = user.AddRefreshToken(newRefreshTokenValue, Guid
            .NewGuid()
            .ToString(), TimeSpan.FromDays(settings.RefreshTokenExpirationDays));

        await dbContext.SaveChangeAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new LoginResponse(newAccessToken, newRefreshTokenValue, DateTime.UtcNow.AddMinutes(settings.AccessTokenExpirationMinutes), newRefreshToken.ExpiresAt, new UserResponse(user.Id, user.Email, user.Name, user.Role.ToString())));
    }
}