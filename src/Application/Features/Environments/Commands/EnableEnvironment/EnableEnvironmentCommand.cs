using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Environments;
using Domain.Exceptions;

namespace Application.Features.Environments.Commands.EnableEnvironment;

public record EnableEnvironmentCommand(Guid Id) : ICommandRequest;

internal class EnableEnvironmentCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<EnableEnvironmentCommand>
{
    public override async Task<Result> Handle(EnableEnvironmentCommand request, CancellationToken cancellationToken)
{
    var environment = await dbContext.Environments.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Environment), request.Id);

    environment.Enable();
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}