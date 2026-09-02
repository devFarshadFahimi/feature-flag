using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Users;
using Domain.Exceptions;

namespace Application.Features.Users.Commands.UpdateUser;

public record UpdateUserCommand(Guid Id, string? Name, string? Email) : ICommandRequest;

internal class UpdateUserCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<UpdateUserCommand>
{
    public override async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
{
    var user = await dbContext.Users.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), request.Id);

    user.UpdateProfile(request.Name, request.Email);
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}