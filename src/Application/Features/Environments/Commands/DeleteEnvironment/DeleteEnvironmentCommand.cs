using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Environments;
using Domain.Exceptions;

namespace Application.Features.Environments.Commands.DeleteEnvironment;

public record DeleteEnvironmentCommand(Guid Id) : ICommandRequest;

internal class DeleteEnvironmentCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<DeleteEnvironmentCommand>
{
    public override async Task<Result> Handle(DeleteEnvironmentCommand request, CancellationToken cancellationToken)
{
    var environment = await dbContext.Environments.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Environment), request.Id);

    dbContext.Environments.Remove(environment);
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}