namespace Application.Features.Users.Commands.CreateUser;

public record CreateUserCommand(string Email, string Password, UserRole Role = UserRole.Viewer, string? Name = null) : ICommandRequest<Guid>;
internal class CreateUserCommandHandler(IApplicationDbContext dbContext, IPasswordHasher passwordHasher, BusinessMakerFramework.Shared.Abstractions.UserManagement.IUserInfoService actor) : CommandRequestHandler<CreateUserCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Role) || string.IsNullOrWhiteSpace(request.Email) || request.Password.Length < 12)
        {
            throw new ArgumentException("ایمیل، گذرواژه یا نقش کاربری معتبر نیست؛ گذرواژه باید حداقل ۱۲ نویسه داشته باشد.");
        }


        var currentRole = await dbContext.Users
            .Where(u => u.Id == actor.Id() && u.IsActive)
            .Select(u => u.Role)
            .SingleAsync(cancellationToken);

        if (currentRole < UserRole.Admin || request.Role == UserRole.RootAdmin && currentRole != UserRole.RootAdmin)
        {
            throw new System.Security.SecurityException("اجازه ایجاد کاربر با این نقش را ندارید.");
        }


        var passwordHash = passwordHasher.HashPassword(request.Password);
        var user = User.Create(request.Email, passwordHash, request.Role, request.Name);

        _ = dbContext.Users.Add(user);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok(user.Id);
    }
}