using Application.Features.Auth.Commands.Login;

namespace Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : ICommandRequest<LoginResponse>;
internal class RefreshTokenCommandHandler(IApplicationDbContext dbContext, IJwtTokenGenerator jwtTokenGenerator, IOptions<JwtSettings> jwtSettings) : CommandRequestHandler<RefreshTokenCommand, LoginResponse>
{
    public override async Task<Result<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Validate access token (even if expired)
        var principal = jwtTokenGenerator.ValidateToken(request.AccessToken);

        if (principal == null)
        {
            throw new UnauthorizedAccessException("توکن دسترسی معتبر نیست.");
        }


        var userId = Guid.Parse(principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("اطلاعات هویتی توکن معتبر نیست."));
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"App\".\"TblUser\" WHERE \"Id\" = {userId} FOR UPDATE", cancellationToken);

        var user = await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");
        var refreshToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken) ?? throw new UnauthorizedAccessException("توکن تمدید معتبر نیست.");

        if (!user.IsActive || principal.FindFirst("security_version")?.Value != user.SecurityVersion.ToString())
        {
            throw new UnauthorizedAccessException("حساب کاربری غیرفعال است یا نشست اعتبار ندارد.");
        }

        if (!refreshToken.IsValid())
        {
            throw new UnauthorizedAccessException("توکن تمدید منقضی، مصرف یا لغو شده است.");
        }

        // Mark old token as used
        refreshToken.MarkAsUsed();
        // Generate new tokens
        var newAccessToken = jwtTokenGenerator.GenerateAccessToken(user);
        var newRefreshTokenValue = jwtTokenGenerator.GenerateRefreshToken();
        var settings = jwtSettings.Value;
        var newRefreshToken = user.AddRefreshToken(newRefreshTokenValue, Guid
            .NewGuid()
            .ToString(), TimeSpan.FromDays(settings.RefreshTokenExpirationDays));

        try
        {
            await dbContext.SaveChangeAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new UnauthorizedAccessException("توکن تمدید قبلاً مصرف شده است.");
        }

        await transaction.CommitAsync(cancellationToken);

        return Ok(new LoginResponse(newAccessToken, newRefreshToken.Token, DateTime.UtcNow.AddMinutes(settings.AccessTokenExpirationMinutes), newRefreshToken.ExpiresAt, new Application.Features.Auth.Commands.Login.UserResponse(user.Id, user.Email, user.Name, user.Role.ToString())));
    }
}