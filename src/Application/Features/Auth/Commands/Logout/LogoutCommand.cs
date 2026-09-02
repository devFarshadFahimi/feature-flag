using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Users;
using Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        ?? throw new EntityNotFoundException(nameof(User), request.UserId);

    user.RevokeAllRefreshTokens();
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}