using BusinessMakerFramework.Shared.Abstractions.UserManagement;

namespace Application.Features.Auth.Commands.Passwords;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommandRequest;
public record ResetUserPasswordCommand(Guid Id, string NewPassword) : ICommandRequest;
internal class ChangePasswordCommandHandler(IApplicationDbContext dbContext, IPasswordHasher passwordHasher, IUserInfoService actor) : CommandRequestHandler<ChangePasswordCommand>
{
    /// <summary>Requires the old password under an account lock and invalidates every existing session atomically.</summary>
    /// <param name = "request">The requested account operation.</param>
    /// <param name = "cancellationToken">Cancels database operations.</param>
    /// <returns>The successful result after persistence.</returns>
    public override async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        PasswordRules.Validate(request.NewPassword);

        var userId = actor.Id();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"App\".\"TblUser\" WHERE \"Id\" = {userId} FOR UPDATE", cancellationToken);

        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken) ?? throw new UnauthorizedAccessException("نشست کاربری معتبر نیست.");

        if (string.IsNullOrEmpty(request.CurrentPassword) || System.Text.Encoding.UTF8.GetByteCount(request.CurrentPassword) > 72 || !passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("گذرواژه فعلی نادرست است.");
        }

        user.UpdatePassword(passwordHasher.HashPassword(request.NewPassword));
        await dbContext.Users
            .Where(u => u.Id == userId)
            .SelectMany(u => u.RefreshTokens)
            .Where(t => !t.IsRevoked)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.IsRevoked, true), cancellationToken);

        await dbContext.SaveChangeAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok();
    }
}

internal class ResetUserPasswordCommandHandler(IApplicationDbContext dbContext, IPasswordHasher passwordHasher, IUserInfoService actor) : CommandRequestHandler<ResetUserPasswordCommand>
{
    /// <summary>Rechecks administrator hierarchy under ordered account locks and forbids bypassing self-service password verification.</summary>
    /// <param name = "request">The requested account operation.</param>
    /// <param name = "cancellationToken">Cancels database operations.</param>
    /// <returns>The successful result after persistence.</returns>
    public override async Task<Result> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        PasswordRules.Validate(request.NewPassword);

        var actorId = actor.Id();

        if (request.Id == actorId)
        {
            throw new System.Security.SecurityException("برای تغییر گذرواژه خود از مسیر تغییر گذرواژه و گذرواژه فعلی استفاده کنید.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"App\".\"TblUser\" WHERE \"Id\" = {actorId} OR \"Id\" = {request.Id} ORDER BY \"Id\" FOR UPDATE", cancellationToken);

        var currentUser = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == actorId && u.IsActive, cancellationToken);
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken) ?? throw new KeyNotFoundException("کاربر موردنظر یافت نشد.");

        if (currentUser is null || currentUser.Role < UserRole.Admin || (user.Role == UserRole.RootAdmin && currentUser.Role != UserRole.RootAdmin))
        {
            throw new System.Security.SecurityException("اجازه بازنشانی گذرواژه این کاربر را ندارید.");
        }

        user.UpdatePassword(passwordHasher.HashPassword(request.NewPassword));
        await dbContext.Users
            .Where(u => u.Id == request.Id)
            .SelectMany(u => u.RefreshTokens)
            .Where(t => !t.IsRevoked)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.IsRevoked, true), cancellationToken);

        await dbContext.SaveChangeAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok();
    }
}

internal static class PasswordRules
{
    internal static void Validate(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 12 || System.Text.Encoding.UTF8.GetByteCount(password) > 72)
        {
            throw new ArgumentException("گذرواژه باید حداقل ۱۲ نویسه و حداکثر ۷۲ بایت با کدگذاری UTF-8 داشته باشد.");
        }
    }
}