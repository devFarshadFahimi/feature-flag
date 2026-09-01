using Domain.ValueObjects;

namespace Infrastructure.Configurations;

/// <summary>
/// Configuration for Constraint value objects owned by a Segment.
/// </summary>
public class SegmentConstraintConfiguration : ApplicationConfiguration<Constraint>
{
    public override void Configure(EntityTypeBuilder<Constraint> builder)
    {
        base.Configure(builder);

        _ = builder.Property<int>("Id")
            .ValueGeneratedOnAdd();
        _ = builder.HasKey("Id");

        // Shadow FK
        _ = builder.Property<int>("SegmentId");

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

        _ = builder.Property(c => c.Values)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                    v, (System.Text.Json.JsonSerializerOptions?)null) ?? new())
            .HasColumnName("Values")
            .HasColumnType("jsonb");

        // Indexes
        _ = builder.HasIndex("SegmentId");

        _ = builder.HasIndex(c => c.ContextName);
    }
}
