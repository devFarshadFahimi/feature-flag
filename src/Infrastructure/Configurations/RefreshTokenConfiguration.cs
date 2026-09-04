using Domain.Aggregates.UserAggregate;

namespace Infrastructure.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        _ = builder.Property<int>("Id")
            .ValueGeneratedOnAdd();
        _ = builder.HasKey("Id");

        _ = builder.Property<Guid>("UserId")
            .IsRequired();

        _ = builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(512);

        _ = builder.Property(rt => rt.JwtId)
            .IsRequired()
            .HasMaxLength(256);

        _ = builder.Property(rt => rt.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        _ = builder.Property(rt => rt.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        _ = builder.Property(rt => rt.CreatedAt)
            .IsRequired();

        _ = builder.Property(rt => rt.ExpiresAt)
            .IsRequired();

        // Indexes
        _ = builder.HasIndex(rt => rt.Token)
            .IsUnique();

        _ = builder.HasIndex("UserId");

        _ = builder.HasIndex(rt => rt.ExpiresAt);
    }
}