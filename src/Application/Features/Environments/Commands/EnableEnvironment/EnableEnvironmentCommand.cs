namespace Application.Features.Environments.Commands.EnableEnvironment;

public record EnableEnvironmentCommand(Guid Id) : ICommandRequest;

internal class EnableEnvironmentCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<EnableEnvironmentCommand>
{
    public override async Task<Result> Handle(EnableEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var environment = await dbContext.Environments.FindAsync([request.Id], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(Environment), request.Id + string.Empty);

        environment.Enable();
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}