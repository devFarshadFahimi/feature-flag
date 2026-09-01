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
            .WithOne()
            .HasForeignKey(f => f.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasMany(p => p.Members)
            .WithOne()
            .HasForeignKey("ProjectId")
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain-only collections that are managed via child configs
        _ = builder.Ignore(p => p.Features);  // if handled via FeatureConfiguration
        _ = builder.Ignore(p => p.Members);   // if handled via ProjectMemberConfiguration
    }
}
