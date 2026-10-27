using Domain.Aggregates.ProjectAggregate;

namespace Infrastructure.Configurations;

public sealed class ProjectEnvironmentPolicyConfiguration : IEntityTypeConfiguration<ProjectEnvironmentPolicy>
{
    public void Configure(EntityTypeBuilder<ProjectEnvironmentPolicy> builder)
    {
        builder.ToTable("ProjectEnvironmentPolicy", PersistenceNames.Schema);
        builder.HasKey(p => new { p.ProjectId, p.EnvironmentId });
        builder
            .Property(p => p.Revision)
            .IsConcurrencyToken();
        builder
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne<Domain.Aggregates.EnvironmentAggregate.Environment>()
            .WithMany()
            .HasForeignKey(p => p.EnvironmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}