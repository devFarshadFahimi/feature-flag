using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Environments;
using Domain.Exceptions;

namespace Application.Features.Environments.Commands.RevokeApiToken;

public record RevokeApiTokenCommand(Guid EnvironmentId, Guid TokenId) : ICommandRequest;

internal class RevokeApiTokenCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<RevokeApiTokenCommand>
{
    public override async Task<Result> Handle(RevokeApiTokenCommand request, CancellationToken cancellationToken)
{
    var environment = await dbContext.Environments.FindAsync([request.EnvironmentId], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Environment), request.EnvironmentId);

    environment.RevokeToken(request.TokenId);
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}