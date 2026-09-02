using Domain.Aggregates.ApiTokenAggregate;

namespace Infrastructure.Configurations;

public class ApiTokenConfiguration : ApplicationConfiguration<ApiToken>
{
    public override void Configure(EntityTypeBuilder<ApiToken> builder)
    {
        base.Configure(builder);

        // Properties
        _ = builder.Property(t => t.EnvironmentId)
            .IsRequired();

        _ = builder.Property(t => t.TokenType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        _ = builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(256);

        _ = builder.Property(t => t.Name)
            .HasMaxLength(255);

        _ = builder.Property(t => t.CreatedAt)
            .IsRequired();

        _ = builder.Property(t => t.ExpiresAt);

        _ = builder.Property(t => t.LastUsedAt);

        _ = builder.Property(t => t.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        _ = builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        _ = builder.HasIndex(t => t.EnvironmentId);

        _ = builder.HasIndex(t => t.TokenType);

        _ = builder.HasIndex(t => t.IsRevoked);

        // Composite for fast validation look-ups
        _ = builder.HasIndex(t => new { t.TokenHash, t.IsRevoked });
    }
}




public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.Property<int>("Id")
            .ValueGeneratedOnAdd();
        builder.HasKey("Id");

        builder.Property<Guid>("UserId")
            .IsRequired();

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(rt => rt.JwtId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(rt => rt.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rt => rt.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rt => rt.CreatedAt)
            .IsRequired();

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        builder.HasIndex("UserId")
            .HasDatabaseName("IX_RefreshTokens_UserId");

        builder.HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("IX_RefreshTokens_ExpiresAt");
    }
}