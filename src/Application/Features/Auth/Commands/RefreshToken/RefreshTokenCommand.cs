using Application.Features.Auth.Commands.Login;

namespace Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : ICommandRequest<LoginResponse>;

internal class RefreshTokenCommandHandler(
    IApplicationDbContext dbContext,
    IJwtTokenGenerator jwtTokenGenerator,
    IOptions<JwtSettings> jwtSettings)
    : CommandRequestHandler<RefreshTokenCommand, LoginResponse>
{
    public override async Task<Result<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Validate access token (even if expired)
        var principal = jwtTokenGenerator.ValidateToken(request.AccessToken);
        if (principal == null)
        {
            throw new InvalidEntityStateException("Invalid access token");
        }

        var userId = Guid.Parse(principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidEntityStateException("Invalid token claims"));

        var user = await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidEntityStateException(nameof(User), userId + string.Empty);

        var refreshToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken)
            ?? throw new InvalidEntityStateException("Invalid refresh token");

        if (!refreshToken.IsValid())
        {
            throw new InvalidEntityStateException("Refresh token is expired, used, or revoked");
        }

        // Mark old token as used
        refreshToken.MarkAsUsed();

        // Generate new tokens
        var newAccessToken = jwtTokenGenerator.GenerateAccessToken(user);
        var newRefreshTokenValue = jwtTokenGenerator.GenerateRefreshToken();

        var settings = jwtSettings.Value;
        var newRefreshToken = user.AddRefreshToken(
            newRefreshTokenValue,
            Guid.NewGuid().ToString(),
            TimeSpan.FromDays(settings.RefreshTokenExpirationDays));

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok(new LoginResponse(
            newAccessToken,
            newRefreshToken.Token,
            DateTime.UtcNow.AddMinutes(settings.AccessTokenExpirationMinutes),
            newRefreshToken.ExpiresAt,
            new Application.Features.Auth.Commands.Login.UserResponse(
                user.Id, user.Email, user.Name, user.Role.ToString())));
    }
}
