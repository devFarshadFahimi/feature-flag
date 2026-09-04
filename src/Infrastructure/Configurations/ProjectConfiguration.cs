using Domain.Aggregates.ProjectAggregate;

namespace Infrastructure.Configurations;

public class ProjectConfiguration : ApplicationConfiguration<Project>
{
    public override void Configure(EntityTypeBuilder<Project> builder)
    {
        base.Configure(builder);

        // Properties
        _ = builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(255);

        _ = builder.Property(p => p.Description)
            .HasMaxLength(2000);

        _ = builder.Property(p => p.DefaultStickiness)
            .IsRequired()
            .HasMaxLength(64)
            .HasDefaultValue("default");

        _ = builder.Property(p => p.FeatureLimitEnabled)
            .IsRequired();

        _ = builder.Property(p => p.FeatureLimit);

        // Indexes
        _ = builder.HasIndex(p => p.Name)
            .IsUnique();

        // Relationships
        _ = builder.HasMany(p => p.Features)
            .WithOne(p => p.Project)
            .HasForeignKey(f => f.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasMany(p => p.Members)
            .WithOne(p => p.Project)
            .HasForeignKey(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasMany(p => p.ChangeRequests)
            .WithOne(p => p.Project)
            .HasForeignKey(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
