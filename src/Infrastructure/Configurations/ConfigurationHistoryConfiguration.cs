using Domain.Aggregates.HistoryAggregate;
using Domain.Aggregates.ProjectAggregate;

namespace Infrastructure.Configurations;

public sealed class ConfigurationHistoryConfiguration : IEntityTypeConfiguration<ConfigurationHistory>
{
    public void Configure(EntityTypeBuilder<ConfigurationHistory> builder)
    {
        builder.ToTable("ConfigurationHistory", PersistenceNames.Schema);
        builder.HasKey(h => h.Id);
        builder
            .Property(h => h.ResourceType)
            .HasMaxLength(128);
        builder
            .Property(h => h.ResourceId)
            .HasMaxLength(128);
        builder
            .Property(h => h.Before)
            .HasColumnType(PersistenceNames.JsonColumnType);
        builder
            .Property(h => h.After)
            .HasColumnType(PersistenceNames.JsonColumnType);
        builder.HasIndex(h => new { h.ProjectId, h.Timestamp });
    }
}