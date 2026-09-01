using Domain.ValueObjects;

namespace Infrastructure.Configurations;

/// <summary>
/// Configuration for Variant value objects (used by FeatureEnvironment and FeatureStrategy).
/// </summary>
public class FeatureVariantConfiguration : ApplicationConfiguration<Variant>
{
    public override void Configure(EntityTypeBuilder<Variant> builder)
    {
        base.Configure(builder);

        _ = builder.Property<int>("Id")
            .ValueGeneratedOnAdd();
        _ = builder.HasKey("Id");

        // Shadow FKs (either FeatureEnvironmentId or StrategyId)
        _ = builder.Property<Guid?>("FeatureEnvironmentId");
        _ = builder.Property<Guid?>("StrategyId");

        // Properties
        _ = builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(128);

        _ = builder.Property(v => v.Weight)
            .IsRequired();

        _ = builder.Property(v => v.Stickiness)
            .HasMaxLength(64);

        _ = builder.Property(v => v.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        // Payload as JSON
        _ = builder.Property(v => v.Payload)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                    v, (System.Text.Json.JsonSerializerOptions?)null) ?? new())
            .HasColumnName("Payload")
            .HasColumnType("jsonb");

        // Indexes
        _ = builder.HasIndex("FeatureEnvironmentId");

        _ = builder.HasIndex("StrategyId");
    }
}
