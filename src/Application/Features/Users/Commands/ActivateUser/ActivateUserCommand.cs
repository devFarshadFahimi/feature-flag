using Domain.Aggregates.UserAggregate;

namespace Application.Features.Users.Commands.ActivateUser;

public record ActivateUserCommand(Guid Id) : ICommandRequest;
internal class ActivateUserCommandHandler(IApplicationDbContext dbContext) : CommandRequestHandler<ActivateUserCommand>
{
    public override async Task<Result> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync([request.Id], cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        user.Activate();

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok();
    }
}