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
        builder
            .Property(batch => batch.Status)
            .HasMaxLength(32)
            .HasDefaultValue(EvaluationBatch.PendingStatus);
        builder
            .Property(batch => batch.FailureReason)
            .HasMaxLength(512);
        builder.HasIndex(batch => new { batch.Status, batch.RecordedAt, batch.Id });
        builder.HasIndex(batch => batch.QuarantinedAt);
    }
}