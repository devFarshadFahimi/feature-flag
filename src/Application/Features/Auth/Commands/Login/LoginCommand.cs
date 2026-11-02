namespace Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : ICommandRequest<LoginResponse>;
public record LoginResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiration, DateTime RefreshTokenExpiration, UserResponse User);
public record UserResponse(Guid Id, string Email, string? Name, string Role);

public sealed class LoginRejectedException : UnauthorizedAccessException
{
    public LoginRejectedException()
        : base("ایمیل یا گذرواژه نادرست است.")
    {
    }
}

internal class LoginCommandHandler(IApplicationDbContext dbContext, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator, IOptions<JwtSettings> jwtSettings) : CommandRequestHandler<LoginCommand, LoginResponse>
{
    /// <summary>Serializes account issuance and commits failed attempts before rejecting authentication.</summary>
    /// <param name = "request">The requested account operation.</param>
    /// <param name = "cancellationToken">Cancels database operations.</param>
    /// <returns>The successful result after persistence.</returns>
    public override async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password) || System.Text.Encoding.UTF8.GetByteCount(request.Password) > 72)
        {
            throw new LoginRejectedException();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var userId = await dbContext.Users
            .Where(u => u.Email == request.Email)
            .Select(u => (Guid?)u.Id)
            .SingleOrDefaultAsync(cancellationToken) ?? throw new LoginRejectedException();

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"App\".\"TblUser\" WHERE \"Id\" = {userId} FOR UPDATE", cancellationToken);

        var user = await dbContext.Users.SingleAsync(u => u.Id == userId, cancellationToken);
        var now = DateTime.UtcNow;

        if (!user.IsActive || user.LockedUntil > now)
        {
            throw new LoginRejectedException();
        }

        if (!passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(now);

            await dbContext.SaveChangeAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            throw new LoginRejectedException();
        }

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user);
        var refreshTokenValue = jwtTokenGenerator.GenerateRefreshToken();
        var settings = jwtSettings.Value;
        var refreshToken = user.AddRefreshToken(refreshTokenValue, Guid
            .NewGuid()
            .ToString(), TimeSpan.FromDays(settings.RefreshTokenExpirationDays));

        user.RecordLogin();

        await dbContext.SaveChangeAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new LoginResponse(accessToken, refreshTokenValue, DateTime.UtcNow.AddMinutes(settings.AccessTokenExpirationMinutes), refreshToken.ExpiresAt, new UserResponse(user.Id, user.Email, user.Name, user.Role.ToString())));
    }
}
