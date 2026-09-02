using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Users;
using Domain.Exceptions;

namespace Application.Features.Users.Commands.DeactivateUser;

public record DeactivateUserCommand(Guid Id) : ICommandRequest;

internal class DeactivateUserCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<DeactivateUserCommand>
{
    public override async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
{
    var user = await dbContext.Users.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), request.Id);

    user.Deactivate();
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}