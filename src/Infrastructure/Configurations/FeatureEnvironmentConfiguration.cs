using Domain.Aggregates.FeatureAggregate;

namespace Infrastructure.Configurations;

public class FeatureEnvironmentConfiguration : ApplicationConfiguration<FeatureEnvironment>
{
    public override void Configure(EntityTypeBuilder<FeatureEnvironment> builder)
    {
        base.Configure(builder);

        // Properties
        _ = builder.Property(fe => fe.FeatureId)
            .IsRequired();

        _ = builder.Property(fe => fe.EnvironmentId)
            .IsRequired();

        _ = builder.Property(fe => fe.Enabled)
            .IsRequired()
            .HasDefaultValue(false);

        _ = builder.Property(fe => fe.LastSeenAt);

        // Indexes
        _ = builder.HasIndex(fe => new { fe.FeatureId, fe.EnvironmentId })
            .IsUnique();

        _ = builder.HasIndex(fe => fe.EnvironmentId);

        // Relationships
        _ = builder.HasMany(fe => fe.Strategies)
            .WithOne()
            .HasForeignKey(s => new { s.FeatureId, s.EnvironmentId })
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.Ignore(fe => fe.Strategies);
        _ = builder.Ignore(fe => fe.Variants); // handled via FeatureVariantConfiguration
    }
}
