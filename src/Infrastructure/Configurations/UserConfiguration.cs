using Domain.Aggregates.UserAggregate;
using Domain.Enums;

namespace Infrastructure.Configurations;

public class UserConfiguration : ApplicationConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        // Properties
        _ = builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        _ = builder.Property(u => u.Name)
            .HasMaxLength(255);

        _ = builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        _ = builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(UserRole.Viewer);

        _ = builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        _ = builder.Property(u => u.CreatedAt)
            .IsRequired();

        _ = builder.Property(u => u.LastLoginAt);

        // Indexes
        _ = builder.HasIndex(u => u.Email)
            .IsUnique();

        _ = builder.HasIndex(u => u.Role);

        _ = builder.HasIndex(u => u.IsActive);
    }
}
