using Domain.Aggregates.FeatureAggregate;

namespace Infrastructure.Configurations;

public class FeatureStrategyConfiguration : ApplicationConfiguration<FeatureStrategy>
{
    public override void Configure(EntityTypeBuilder<FeatureStrategy> builder)
    {
        base.Configure(builder);

        // Properties
        _ = builder.Property(s => s.FeatureId)
            .IsRequired();

        _ = builder.Property(s => s.EnvironmentId)
            .IsRequired();

        _ = builder.Property(s => s.Type)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        _ = builder.Property(s => s.SortOrder)
            .HasDefaultValue(0);

        // StrategyParameters as owned type (stored as JSON)
        _ = builder.OwnsOne(s => s.Parameters, p =>
        {
            _ = p.Property(pp => pp.RolloutPercentage).HasColumnName("RolloutPercentage");
            _ = p.Property(pp => pp.Stickiness).HasColumnName("Stickiness").HasMaxLength(64);
            _ = p.Property(pp => pp.GroupId).HasColumnName("GroupId").HasMaxLength(128);

            _ = p.Property(pp => pp.UserIds)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                        v, (System.Text.Json.JsonSerializerOptions?)null) ?? new())
                .HasColumnName("UserIds")
                .HasColumnType("jsonb");

            _ = p.Property(pp => pp.IpAddresses)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                        v, (System.Text.Json.JsonSerializerOptions?)null) ?? new())
                .HasColumnName("IpAddresses")
                .HasColumnType("jsonb");

            _ = p.Property(pp => pp.ApplicationNames)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                        v, (System.Text.Json.JsonSerializerOptions?)null) ?? new())
                .HasColumnName("ApplicationNames")
                .HasColumnType("jsonb");

            _ = p.Property(pp => pp.CustomParameters)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                        v, (System.Text.Json.JsonSerializerOptions?)null) ?? new())
                .HasColumnName("CustomParameters")
                .HasColumnType("jsonb");
        });

        // Segment IDs as JSON array
        _ = builder.Property(s => s.SegmentIds)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<int>>(
                    v, (System.Text.Json.JsonSerializerOptions?)null) ?? new())
            .HasColumnName("SegmentIds")
            .HasColumnType("jsonb");

        // Indexes
        _ = builder.HasIndex(s => new { s.FeatureId, s.EnvironmentId });

        _ = builder.HasIndex(s => s.Type);

        // Relationships
        _ = builder.HasMany(s => s.Constraints)
            .WithOne()
            .HasForeignKey("StrategyId")
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasMany(s => s.Variants)
            .WithOne()
            .HasForeignKey("StrategyId")
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.Ignore(s => s.Constraints);
        _ = builder.Ignore(s => s.Variants);
    }
}
