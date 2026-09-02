using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Users;
using Domain.Enums;
using Domain.Exceptions;

namespace Application.Features.Users.Commands.PromoteUser;

public record PromoteUserCommand(Guid Id, UserRole Role) : ICommandRequest;

internal class PromoteUserCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<PromoteUserCommand>
{
    public override async Task<Result> Handle(PromoteUserCommand request, CancellationToken cancellationToken)
{
    var user = await dbContext.Users.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), request.Id);

    user.PromoteTo(request.Role);
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}