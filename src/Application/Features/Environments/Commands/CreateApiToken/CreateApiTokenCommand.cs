namespace Application.Features.Environments.Commands.CreateApiToken;

public record CreateApiTokenCommand(Guid EnvironmentId, ApiTokenType TokenType, string? Name = null, DateTime? ExpiresAt = null) : ICommandRequest<Guid>;

internal class CreateApiTokenCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<CreateApiTokenCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(CreateApiTokenCommand request, CancellationToken cancellationToken)
    {
        var environment = await dbContext.Environments.FirstOrDefaultAsync(p => p.Id == request.EnvironmentId, cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(Environment), request.EnvironmentId + string.Empty);

        var tokenHash = Guid.NewGuid().ToString("N"); // In production, use proper token generation
        var token = ApiToken.Create(environment.Id, request.TokenType, tokenHash, request.Name, request.ExpiresAt);
        _ = await dbContext.ApiTokens.AddAsync(token, cancellationToken);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok(token.Id);
    }
}