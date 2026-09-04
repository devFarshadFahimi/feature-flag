using Domain.Aggregates.UserAggregate;

namespace Application.Features.Users.Commands.DeactivateUser;

public record DeactivateUserCommand(Guid Id) : ICommandRequest;

internal class DeactivateUserCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<DeactivateUserCommand>
{
    public override async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync([request.Id], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(User), request.Id + string.Empty);

        user.Deactivate();
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}