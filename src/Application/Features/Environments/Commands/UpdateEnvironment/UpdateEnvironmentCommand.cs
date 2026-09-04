namespace Application.Features.Environments.Commands.UpdateEnvironment;

public record UpdateEnvironmentCommand(Guid Id, string Name, int SortOrder) : ICommandRequest;

internal class UpdateEnvironmentCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<UpdateEnvironmentCommand>
{
    public override async Task<Result> Handle(UpdateEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var environment = await dbContext.Environments.FindAsync([request.Id], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(Environment), request.Id + string.Empty);

        environment.UpdateSortOrder(request.SortOrder);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}