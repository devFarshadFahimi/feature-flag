using Domain.Aggregates.RateLimitAggregate;

namespace Infrastructure.Configurations;

public sealed class RateLimitRejectionSummaryConfiguration : IEntityTypeConfiguration<RateLimitRejectionSummary>
{
    public void Configure(EntityTypeBuilder<RateLimitRejectionSummary> builder)
    {
        builder.ToTable(nameof(RateLimitRejectionSummary), PersistenceNames.Schema);
        builder.HasKey(summary => new { summary.BucketKey, summary.Minute });
        builder
            .Property(summary => summary.BucketKey)
            .HasMaxLength(64);
        builder
            .Property(summary => summary.IpAddress)
            .HasMaxLength(45);
        builder
            .Property(summary => summary.Application)
            .HasMaxLength(128);
        builder
            .Property(summary => summary.Environment)
            .HasMaxLength(64);
        builder
            .Property(summary => summary.Method)
            .HasMaxLength(16);
        builder
            .Property(summary => summary.Route)
            .HasMaxLength(1024);
        builder.HasIndex(summary => summary.Minute);
        builder.HasIndex(summary => new { summary.IpAddress, summary.Minute });
        builder.HasIndex(summary => new { summary.Application, summary.Environment, summary.Minute });
    }
}