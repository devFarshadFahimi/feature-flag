using Domain.Aggregates.UsageAggregate;
using Domain.Aggregates.ProjectAggregate;

namespace Infrastructure.Configurations;

public sealed class FlagUsageConfiguration : IEntityTypeConfiguration<FlagUsage>
{
    public void Configure(EntityTypeBuilder<FlagUsage> builder)
    {
        builder.ToTable("FlagUsage", PersistenceNames.Schema);
        builder.HasKey(u => new { u.ProjectId, u.EnvironmentId, u.AppName, u.FeatureName, u.Variant, u.Enabled, u.Impression, u.Hour });
        builder
            .Property(u => u.AppName)
            .HasMaxLength(128);
        builder
            .Property(u => u.FeatureName)
            .HasMaxLength(255);
        builder
            .Property(u => u.Variant)
            .HasMaxLength(128);
        builder.HasIndex(u => u.Hour);
    }
}