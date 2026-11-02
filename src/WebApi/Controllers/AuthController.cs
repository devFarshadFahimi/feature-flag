using System.Security.Claims;
using Application.Features.Auth.Commands.Login;
using Application.Features.Auth.Commands.Logout;
using Application.Features.Auth.Commands.Passwords;
using Application.Features.Auth.Commands.RefreshToken;
using Application.Features.Auth.Queries.GetCurrentUser;

namespace WebApi.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        return await SendAsync(command, cancellationToken);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        return await SendAsync(command, cancellationToken);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        return await SendAsync(command, cancellationToken);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse((User.FindFirst(Application.Common.Services.IdentityClaimNames.Subject) ?? User.FindFirst(ClaimTypes.NameIdentifier))?.Value ?? throw new UnauthorizedAccessException("نشست کاربری معتبر نیست."));

        return await SendAsync(new LogoutCommand(userId), cancellationToken);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse((User.FindFirst(Application.Common.Services.IdentityClaimNames.Subject) ?? User.FindFirst(ClaimTypes.NameIdentifier))?.Value ?? throw new UnauthorizedAccessException("نشست کاربری معتبر نیست."));

        return await SendAsync(new GetCurrentUserQuery(userId), cancellationToken);
    }
}