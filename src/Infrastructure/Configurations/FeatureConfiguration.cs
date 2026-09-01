using Domain.Aggregates.FeatureAggregate;

namespace Infrastructure.Configurations;

public class FeatureConfiguration : ApplicationConfiguration<Feature>
{
    public override void Configure(EntityTypeBuilder<Feature> builder)
    {
        base.Configure(builder);

        // Properties
        _ = builder.Property(f => f.ProjectId)
            .IsRequired();

        _ = builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(255);

        _ = builder.Property(f => f.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        _ = builder.Property(f => f.Description)
            .HasMaxLength(2000);

        _ = builder.Property(f => f.Lifecycle)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(FeatureLifecycle.Planned);

        _ = builder.Property(f => f.IsStale)
            .IsRequired()
            .HasDefaultValue(false);

        _ = builder.Property(f => f.ImpressionDataEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        _ = builder.Property(f => f.CreatedAt)
            .IsRequired();

        _ = builder.Property(f => f.ArchivedAt);

        // Indexes
        _ = builder.HasIndex(f => new { f.ProjectId, f.Name })
            .IsUnique()
            .HasFilter("\"ArchivedAt\" IS NULL");

        _ = builder.HasIndex(f => f.Name)
            .IsUnique();

        _ = builder.HasIndex(f => f.ProjectId);

        _ = builder.HasIndex(f => f.Lifecycle);

        // Tags stored as JSON array
        _ = builder.Property(f => f.Tags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                    v, (System.Text.Json.JsonSerializerOptions?)null) ?? new())
            .HasColumnType("jsonb");

        // Relationships
        _ = builder.HasMany(f => f.Environments)
            .WithOne()
            .HasForeignKey(e => e.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.Ignore(f => f.Environments);
        _ = builder.Ignore(f => f.Tags); // handled via JSON column above
    }
}
