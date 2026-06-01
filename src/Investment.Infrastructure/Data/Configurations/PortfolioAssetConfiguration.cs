using Investment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Investment.Infrastructure.Data.Configurations;

public class PortfolioAssetConfiguration : IEntityTypeConfiguration<PortfolioAsset>
{
    public void Configure(EntityTypeBuilder<PortfolioAsset> builder)
    {
        builder.HasKey(pa => pa.PortfolioAssetId);

        builder.HasOne(pa => pa.Portfolio)
            .WithMany(p => p.PortfolioAssets)
            .HasForeignKey(pa => pa.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pa => pa.Asset)
            .WithMany(a => a.PortfolioAssets)
            .HasForeignKey(pa => pa.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pa => new { pa.PortfolioId, pa.AssetId }).IsUnique();
    }
}
