using Domain.Aggregates.ChangeRequestAggregate;

namespace Infrastructure.Configurations;

public class ChangeRequestConfiguration : ApplicationConfiguration<ChangeRequest>
{
    public override void Configure(EntityTypeBuilder<ChangeRequest> builder)
    {
        base.Configure(builder);

        // Properties
        _ = builder.Property(cr => cr.ProjectId)
            .IsRequired();

        _ = builder.Property(cr => cr.EnvironmentId)
            .IsRequired();

        _ = builder.Property(cr => cr.CreatedBy)
            .IsRequired();

        _ = builder.Property(cr => cr.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(ChangeRequestStatus.Draft);

        _ = builder.Property(cr => cr.Title)
            .HasMaxLength(255);

        _ = builder.Property(cr => cr.Description)
            .HasMaxLength(4000);

        _ = builder.Property(cr => cr.ScheduledAt);

        _ = builder.Property(cr => cr.CreatedAt)
            .IsRequired();

        _ = builder.Property(cr => cr.ReviewedAt);

        _ = builder.Property(cr => cr.ReviewedBy);

        // Indexes
        _ = builder.HasIndex(cr => cr.ProjectId);

        _ = builder.HasIndex(cr => cr.EnvironmentId);

        _ = builder.HasIndex(cr => cr.Status);

        _ = builder.HasIndex(cr => cr.CreatedBy);

        _ = builder.HasIndex(cr => new { cr.ProjectId, cr.Status });

        _ = builder.HasIndex(cr => cr.ScheduledAt)
            .HasFilter("\"ScheduledAt\" IS NOT NULL");

        // Relationships
        _ = builder.HasMany(cr => cr.Items)
            .WithOne(p => p.ChangeRequest)
            .HasForeignKey(i => i.ChangeRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.PrimitiveCollection(p => p.Reviewers);

        //_ = builder.Ignore(cr => cr.Reviewers); // handled via separate table
    }
}
