using Domain.Aggregates.UsageAggregate;

namespace Infrastructure.Configurations;

public sealed class EvaluationBatchConfiguration : IEntityTypeConfiguration<EvaluationBatch>
{
    public void Configure(EntityTypeBuilder<EvaluationBatch> builder)
    {
        builder.ToTable("EvaluationBatch", PersistenceNames.Schema);
        builder.HasKey(batch => batch.Id);
        builder
            .Property(batch => batch.AppName)
            .HasMaxLength(128);
        builder
            .Property(batch => batch.Counts)
            .HasColumnType(PersistenceNames.JsonColumnType);
        builder.HasIndex(batch => new { batch.RecordedAt, batch.Id });
    }
}