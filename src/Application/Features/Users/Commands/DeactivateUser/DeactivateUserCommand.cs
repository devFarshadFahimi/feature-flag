using Domain.Aggregates.UserAggregate;

namespace Application.Features.Users.Commands.DeactivateUser;

public record DeactivateUserCommand(Guid Id) : ICommandRequest;
internal class DeactivateUserCommandHandler(IApplicationDbContext dbContext, BusinessMakerFramework.Shared.Abstractions.UserManagement.IUserInfoService actor) : CommandRequestHandler<DeactivateUserCommand>
{
    public override async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
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

        if (currentRole < UserRole.Admin || (user.Role == UserRole.RootAdmin) && currentRole != UserRole.RootAdmin)
        {
            throw new System.Security.SecurityException("تغییر مدیر ریشه فقط برای مدیر ریشه مجاز است.");
        }

        if (user.IsActive && user.Role == UserRole.RootAdmin && await dbContext.Users.CountAsync(u => u.Role == UserRole.RootAdmin && u.IsActive, cancellationToken) <= 1)
        {
            throw new InvalidEntityStateException("حذف یا غیرفعال‌سازی آخرین مدیر ریشه مجاز نیست.");
        }

        user.Deactivate();

        await dbContext.SaveChangeAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok();
    }
}