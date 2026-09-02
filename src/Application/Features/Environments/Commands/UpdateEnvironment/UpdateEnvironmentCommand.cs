using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Environments;
using Domain.Exceptions;

namespace Application.Features.Environments.Commands.UpdateEnvironment;

public record UpdateEnvironmentCommand(Guid Id, string Name, int SortOrder) : ICommandRequest;

internal class UpdateEnvironmentCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<UpdateEnvironmentCommand>
{
    public override async Task<Result> Handle(UpdateEnvironmentCommand request, CancellationToken cancellationToken)
{
    var environment = await dbContext.Environments.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Environment), request.Id);

    environment.UpdateSortOrder(request.SortOrder);
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}