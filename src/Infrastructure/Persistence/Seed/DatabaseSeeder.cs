using Application.Common.Interfaces;
using Application.Common.Services;
using Domain.Aggregates.UserAggregate;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (!configuration.GetValue<bool>("Bootstrap:Enabled"))
        {
            return;
        }


        var email = configuration["Bootstrap:Email"];
        var password = configuration["Bootstrap:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < 12)
        {
            throw new InvalidOperationException("ایمیل و گذرواژه امن برای راه‌اندازی اولیه الزامی است.");
        }


        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(8610043)");

        if (!await db.Users.AnyAsync(u => u.Role == UserRole.RootAdmin && u.IsActive))
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            db.Users.Add(User.Create(email, hasher.HashPassword(password), UserRole.RootAdmin));

            await db.SaveChangeAsync();
        }


        await transaction.CommitAsync();
    }
}