using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Environments;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Environments.Commands.DisableEnvironment;

public record DisableEnvironmentCommand(Guid Id) : ICommandRequest;

internal class DisableEnvironmentCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<DisableEnvironmentCommand>
{
    public override async Task<Result> Handle(DisableEnvironmentCommand request, CancellationToken cancellationToken)
{
    var environment = await dbContext.Environments.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Environment), request.Id);

    environment.Disable();
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}