namespace Application.Features.Users.Commands.CreateUser;

public record CreateUserCommand(string Email, string Password, UserRole Role = UserRole.Viewer, string? Name = null) : ICommandRequest<Guid>;

internal class CreateUserCommandHandler(IApplicationDbContext dbContext, IPasswordHasher passwordHasher)
    : CommandRequestHandler<CreateUserCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var passwordHash = passwordHasher.HashPassword(request.Password);
        var user = User.Create(request.Email, passwordHash, request.Role, request.Name);

        _ = dbContext.Users.Add(user);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok(user.Id);
    }
}