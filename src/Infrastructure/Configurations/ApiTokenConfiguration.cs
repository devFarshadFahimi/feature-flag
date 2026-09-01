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
