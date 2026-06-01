using Investment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Investment.Infrastructure.Data.Configurations;

public class PriceFetchLogConfiguration : IEntityTypeConfiguration<PriceFetchLog>
{
    public void Configure(EntityTypeBuilder<PriceFetchLog> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Mode).HasMaxLength(20);
        builder.Property(l => l.Errors).HasMaxLength(2000);
        builder.Property(l => l.FetchDate).HasDefaultValueSql("GETUTCDATE()");
    }
}
