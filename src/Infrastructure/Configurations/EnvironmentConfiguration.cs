namespace Infrastructure.Configurations;

public class EnvironmentConfiguration : ApplicationConfiguration<Domain.Aggregates.EnvironmentAggregate.Environment>
{
    public override void Configure(EntityTypeBuilder<Domain.Aggregates.EnvironmentAggregate.Environment> builder)
    {
        base.Configure(builder);

        // Properties
        _ = builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(128);

        _ = builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        _ = builder.Property(e => e.Enabled)
            .IsRequired()
            .HasDefaultValue(true);

        _ = builder.Property(e => e.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        _ = builder.Property(e => e.Protected)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        _ = builder.HasIndex(e => e.Name)
            .IsUnique();

        _ = builder.HasIndex(e => e.Type);

        _ = builder.HasIndex(e => e.SortOrder);

        // Tokens handled via separate aggregate
        _ = builder.Ignore(e => e.Tokens);
    }
}
