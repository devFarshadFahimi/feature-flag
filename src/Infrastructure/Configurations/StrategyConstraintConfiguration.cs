using Domain.ValueObjects;

namespace Infrastructure.Configurations;

/// <summary>
/// Configuration for Constraint value objects stored as a separate table
/// (used by both FeatureStrategy and Segment).
/// </summary>
public class StrategyConstraintConfiguration : ApplicationConfiguration<Constraint>
{
    public override void Configure(EntityTypeBuilder<Constraint> builder)
    {
        base.Configure(builder);

        // No PK from ValueObject base — use surrogate key via shadow property
        _ = builder.Property<int>("Id")
            .ValueGeneratedOnAdd();
        _ = builder.HasKey("Id");

        // Shadow FK
        _ = builder.Property<Guid>("StrategyId");

        // Properties
        _ = builder.Property(c => c.ContextName)
            .IsRequired()
            .HasMaxLength(128);

        _ = builder.Property(c => c.Operator)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        _ = builder.Property(c => c.Inverted)
            .IsRequired()
            .HasDefaultValue(false);

        _ = builder.Property(c => c.CaseInsensitive)
            .IsRequired()
            .HasDefaultValue(false);

        // Values as JSON array
        _ = builder.Property(c => c.Values)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                    v, (System.Text.Json.JsonSerializerOptions?)null) ?? new())
            .HasColumnName("Values")
            .HasColumnType("jsonb");

        // Indexes
        _ = builder.HasIndex("StrategyId");

        _ = builder.HasIndex(c => c.ContextName);
    }
}
