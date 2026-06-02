using Investment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Investment.Infrastructure.Data.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.HasKey(a => a.AssetId);
        builder.Property(a => a.AssetCode).HasMaxLength(50).IsRequired();
        builder.Property(a => a.AssetName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.AssetType).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Currency).HasMaxLength(10).HasDefaultValue("EGP");
        builder.Property(a => a.ExternalTicker).HasMaxLength(20);
        builder.Property(a => a.Notes).HasMaxLength(500);
        builder.Property(a => a.IsDailyAccrualFund).HasDefaultValue(false);
        builder.Property(a => a.DailyAccrualAnnualRatePercent).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(a => a.GoldCashbackPerGram).HasPrecision(18, 5).HasDefaultValue(28.5m);
        builder.Property(a => a.ClosedRealizedPnL).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(a => a.IsActive).HasDefaultValue(true);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(a => a.AssetCode).IsUnique();
    }
}
