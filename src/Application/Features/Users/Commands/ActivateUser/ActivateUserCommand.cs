using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Users;
using Domain.Exceptions;

namespace Application.Features.Users.Commands.ActivateUser;

public record ActivateUserCommand(Guid Id) : ICommandRequest;

internal class ActivateUserCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<ActivateUserCommand>
{
    public override async Task<Result> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
{
    var user = await dbContext.Users.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), request.Id);

    user.Activate();
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}