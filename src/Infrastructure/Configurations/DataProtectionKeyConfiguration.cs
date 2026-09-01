using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace Infrastructure.Configurations;

public class DataProtectionKeyConfiguration : BaseConfiguration<DataProtectionKey>
{
    public override void Configure(EntityTypeBuilder<DataProtectionKey> builder)
    {
        base.Configure(builder);
        builder.Property(p => p.FriendlyName).HasMaxLength(short.MaxValue);
        builder.Property(p => p.Xml).HasMaxLength(short.MaxValue);
    }
}
