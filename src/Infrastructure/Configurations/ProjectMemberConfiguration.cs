using Domain.Aggregates.ProjectAggregate;

namespace Infrastructure.Configurations;

public class ProjectMemberConfiguration : ApplicationConfiguration<ProjectMember>
{
    public override void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        base.Configure(builder);

        // Properties
        _ = builder.Property(pm => pm.UserId)
            .IsRequired();

        _ = builder.Property(pm => pm.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        // Shadow FK
        _ = builder.Property<Guid>("ProjectId")
            .IsRequired();

        // Indexes
        _ = builder.HasIndex("ProjectId", nameof(ProjectMember.UserId))
            .IsUnique();

        _ = builder.HasIndex(pm => pm.UserId);
    }
}
