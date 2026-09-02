using Application.Common.Interfaces;
using Domain.Aggregates.Users;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Check if admin user already exists
            var existingAdmin = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == "admin@featureflags.com");

            if (existingAdmin != null)
            {
                logger.LogInformation("Admin user already exists");
                return;
            }

            // Create admin user
            var passwordHash = passwordHasher.HashPassword("Admin@123456");
            var adminUser = User.Create(
                "admin@featureflags.com",
                passwordHash,
                UserRole.RootAdmin,
                "System Administrator");

            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangeAsync(CancellationToken.None);

            logger.LogInformation("Admin user seeded successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding admin user");
            throw;
        }
    }
}