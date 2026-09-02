using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Users;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : ICommandRequest<LoginResponse>;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiration,
    DateTime RefreshTokenExpiration,
    UserResponse User);

public record UserResponse(
    Guid Id,
    string Email,
    string? Name,
    string Role);

internal class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IOptions<JwtSettings> jwtSettings)
    : CommandRequestHandler<LoginCommand, LoginResponse>
{
    public override async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
{
    var user = await dbContext.Users
        .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
        ?? throw new DomainException("Invalid email or password");

    if (!user.IsActive)
        throw new DomainException("User account is deactivated");

    if (!passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        throw new DomainException("Invalid email or password");

    var accessToken = jwtTokenGenerator.GenerateAccessToken(user);
    var refreshTokenValue = jwtTokenGenerator.GenerateRefreshToken();

    var settings = jwtSettings.Value;
    var refreshToken = user.AddRefreshToken(
        refreshTokenValue,
        Guid.NewGuid().ToString(),
        TimeSpan.FromDays(settings.RefreshTokenExpirationDays));

    user.RecordLogin();
    await dbContext.SaveChangeAsync(cancellationToken);

    return Ok(new LoginResponse(
        accessToken,
        refreshToken.Token,
        DateTime.UtcNow.AddMinutes(settings.AccessTokenExpirationMinutes),
        refreshToken.ExpiresAt,
        new UserResponse(user.Id, user.Email, user.Name, user.Role.ToString())));
}
}