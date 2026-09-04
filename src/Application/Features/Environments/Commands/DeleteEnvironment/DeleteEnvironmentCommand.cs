namespace Application.Features.Environments.Commands.DeleteEnvironment;

public record DeleteEnvironmentCommand(Guid Id) : ICommandRequest;

internal class DeleteEnvironmentCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<DeleteEnvironmentCommand>
{
    public override async Task<Result> Handle(DeleteEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var environment = await dbContext.Environments.FindAsync([request.Id], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(Environment), request.Id + string.Empty);

        _ = dbContext.Environments.Remove(environment);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}