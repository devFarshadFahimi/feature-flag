using Infrastructure.RateLimiting;

namespace Infrastructure.Configurations;

public sealed class RateLimitHistoryCheckpointConfiguration : IEntityTypeConfiguration<RateLimitHistoryCheckpoint>
{
    public void Configure(EntityTypeBuilder<RateLimitHistoryCheckpoint> builder)
    {
        builder.ToTable(nameof(RateLimitHistoryCheckpoint), PersistenceNames.Schema);
        builder.HasKey(checkpoint => checkpoint.StreamKey);
        builder
            .Property(checkpoint => checkpoint.StreamKey)
            .HasMaxLength(256);
        builder
            .Property(checkpoint => checkpoint.LastStreamId)
            .HasMaxLength(64);
    }
}