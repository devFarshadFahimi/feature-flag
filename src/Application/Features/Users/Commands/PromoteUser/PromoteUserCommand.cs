namespace Application.Features.Users.Commands.PromoteUser;

public record PromoteUserCommand(Guid Id, UserRole Role) : ICommandRequest;
internal class PromoteUserCommandHandler(IApplicationDbContext dbContext, BusinessMakerFramework.Shared.Abstractions.UserManagement.IUserInfoService actor) : CommandRequestHandler<PromoteUserCommand>
{
    public override async Task<Result> Handle(PromoteUserCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(8610043)", cancellationToken);

        var user = await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");
        var currentRole = await dbContext.Users
            .Where(u => u.Id == actor.Id() && u.IsActive)
            .Select(u => u.Role)
            .SingleAsync(cancellationToken);

        if (currentRole < UserRole.Admin || (user.Role == UserRole.RootAdmin || request.Role == UserRole.RootAdmin) && currentRole != UserRole.RootAdmin)
        {
            throw new System.Security.SecurityException("تغییر مدیر ریشه فقط برای مدیر ریشه مجاز است.");
        }

        if (user.IsActive && user.Role == UserRole.RootAdmin && request.Role != UserRole.RootAdmin && await dbContext.Users.CountAsync(u => u.Role == UserRole.RootAdmin && u.IsActive, cancellationToken) <= 1)
        {
            throw new InvalidEntityStateException("حذف یا غیرفعال‌سازی آخرین مدیر ریشه مجاز نیست.");
        }

        if (!Enum.IsDefined(request.Role))
        {
            throw new ArgumentException("نقش کاربری معتبر نیست.");
        }

        user.PromoteTo(request.Role);

        await dbContext.SaveChangeAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok();
    }
}