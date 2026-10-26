namespace Application.Features.Environments.Commands.RevokeApiToken;

public record RevokeApiTokenCommand(Guid EnvironmentId, Guid TokenId) : ICommandRequest;
internal class RevokeApiTokenCommandHandler(IApplicationDbContext dbContext) : CommandRequestHandler<RevokeApiTokenCommand>
{
    public override async Task<Result> Handle(RevokeApiTokenCommand request, CancellationToken cancellationToken)
    {
        var environment = await dbContext.Environments
            .Include(e => e.Tokens)
            .FirstOrDefaultAsync(e => e.Id == request.EnvironmentId, cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        environment.RevokeToken(request.TokenId);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok();
    }
}