using Domain.Aggregates.UserAggregate;

namespace Application.Features.Users.Commands.UpdateUser;

public record UpdateUserCommand(Guid Id, string? Name, string? Email) : ICommandRequest;

internal class UpdateUserCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<UpdateUserCommand>
{
    public override async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync([request.Id], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(User), request.Id + string.Empty);

        user.UpdateProfile(request.Name, request.Email);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}