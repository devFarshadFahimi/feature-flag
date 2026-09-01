using Domain.Aggregates.ChangeRequestAggregate;

namespace Infrastructure.Configurations;

public class ChangeRequestItemConfiguration : ApplicationConfiguration<ChangeRequestItem>
{
    public override void Configure(EntityTypeBuilder<ChangeRequestItem> builder)
    {
        base.Configure(builder);

        // Properties
        _ = builder.Property(i => i.ChangeRequestId)
            .IsRequired();

        _ = builder.Property(i => i.Action)
            .IsRequired()
            .HasMaxLength(64);

        _ = builder.Property(i => i.FeatureId);

        _ = builder.Property(i => i.Payload)
            .HasColumnType("jsonb");

        // Indexes
        _ = builder.HasIndex(i => i.ChangeRequestId);

        _ = builder.HasIndex(i => i.FeatureId);

        _ = builder.HasIndex(i => i.Action);
    }
}
