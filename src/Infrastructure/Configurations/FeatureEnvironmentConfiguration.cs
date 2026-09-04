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
            .WithOne(p => p.FeatureEnvironment)
            .HasForeignKey(s => s.FeatureEnvironmentId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasMany(fe => fe.Variants)
            .WithOne()
            .HasForeignKey("StrategyId")
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasMany(fe => fe.Variants)
            .WithOne()
            .HasForeignKey("FeatureEnvironmentId")
            .OnDelete(DeleteBehavior.Cascade);

    }
}
