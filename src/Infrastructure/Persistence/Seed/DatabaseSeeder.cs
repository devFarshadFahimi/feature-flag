using Application.Common.Interfaces;
using Application.Common.Services;
using Domain.Aggregates.UserAggregate;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    private const string EnabledKey = "Bootstrap:Enabled";

    private const string EmailKey = "Bootstrap:Email";

    private const string PasswordKey = "Bootstrap:Password";

    /// <summary>Creates the explicitly configured login once, preserving existing accounts and the root-admin guard.</summary>
    public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (!configuration.GetValue<bool>(EnabledKey))
        {
            return;
        }

        var email = configuration[EmailKey];
        var password = configuration[PasswordKey];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < 12)
        {
            throw new InvalidOperationException("ایمیل و گذرواژه امن برای راه‌اندازی اولیه الزامی است.");
        }

        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(8610043)");

        if (!await db.Users.AnyAsync(u => u.Email == email))
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var role = await db.Users.AnyAsync(u => u.Role == UserRole.RootAdmin && u.IsActive) ? UserRole.Admin : UserRole.RootAdmin;

            db.Users.Add(User.Create(email, hasher.HashPassword(password), role));

            await db.SaveChangeAsync();
        }

        await transaction.CommitAsync();
    }
}