using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Environments;
using Domain.Enums;
using Domain.Exceptions;

namespace Application.Features.Environments.Commands.CreateApiToken;

public record CreateApiTokenCommand(Guid EnvironmentId, ApiTokenType TokenType, string? Name = null, DateTime? ExpiresAt = null) : ICommandRequest<Guid>;

internal class CreateApiTokenCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<CreateApiTokenCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(CreateApiTokenCommand request, CancellationToken cancellationToken)
{
    var environment = await dbContext.Environments.FindAsync([request.EnvironmentId], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Environment), request.EnvironmentId);

    var tokenHash = Guid.NewGuid().ToString("N"); // In production, use proper token generation
    var token = environment.CreateToken(request.TokenType, request.Name, request.ExpiresAt);
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok(token.Id);
}
}