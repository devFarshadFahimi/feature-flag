using Domain.Aggregates.HistoryAggregate;

namespace Infrastructure.Configurations;

public sealed class ConfigurationVersionConfiguration : IEntityTypeConfiguration<ConfigurationVersion>
{
    public void Configure(EntityTypeBuilder<ConfigurationVersion> builder)
    {
        builder.ToTable("ConfigurationVersion", PersistenceNames.Schema);
        builder.HasKey(version => version.Id);
        builder.HasData(new ConfigurationVersion { Id = 1, Revision = 0 });
    }
}