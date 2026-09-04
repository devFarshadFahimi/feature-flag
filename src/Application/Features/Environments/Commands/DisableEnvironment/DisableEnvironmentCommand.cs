namespace Application.Features.Environments.Commands.DisableEnvironment;

public record DisableEnvironmentCommand(Guid Id) : ICommandRequest;

internal class DisableEnvironmentCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<DisableEnvironmentCommand>
{
    public override async Task<Result> Handle(DisableEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var environment = await dbContext.Environments.FindAsync([request.Id], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(Environment), request.Id + string.Empty);

        environment.Disable();
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}