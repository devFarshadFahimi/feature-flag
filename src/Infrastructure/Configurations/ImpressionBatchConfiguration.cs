using Domain.Aggregates.UsageAggregate;
using Domain.Aggregates.ProjectAggregate;

namespace Infrastructure.Configurations;

public sealed class ImpressionBatchConfiguration : IEntityTypeConfiguration<ImpressionBatch>
{
    public void Configure(EntityTypeBuilder<ImpressionBatch> builder)
    {
        builder.ToTable("ImpressionBatch", PersistenceNames.Schema);
        builder.HasKey(b => new { b.ProjectId, b.EnvironmentId, b.BatchId });
        builder.HasIndex(b => b.ReceivedAt);
    }
}