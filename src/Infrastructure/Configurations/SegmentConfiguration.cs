using Domain.Aggregates.SegmentAggregate;

namespace Infrastructure.Configurations;

public class SegmentConfiguration : ApplicationConfiguration<Segment>
{
    public override void Configure(EntityTypeBuilder<Segment> builder)
    {
        base.Configure(builder);

        // Properties
        _ = builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(128);

        _ = builder.Property(s => s.Description)
            .HasMaxLength(2000);

        _ = builder.Property(s => s.IsPublic)
            .IsRequired()
            .HasDefaultValue(true);

        _ = builder.Property(s => s.CreatedAt)
            .IsRequired();

        _ = builder.Property(s => s.LastUsedAt);

        // Indexes
        _ = builder.HasIndex(s => s.Name)
            .IsUnique();

        _ = builder.HasIndex(s => s.IsPublic);

        // Constraints handled via separate table
        _ = builder.Ignore(s => s.Constraints);
    }
}
