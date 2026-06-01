using Investment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Investment.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.TransactionId);
        builder.Property(t => t.TransactionType).HasConversion<string>().HasMaxLength(10);
        builder.Property(t => t.Quantity).HasPrecision(18, 4);
        builder.Property(t => t.PricePerUnit).HasPrecision(18, 5);
        builder.Property(t => t.TotalAmount).HasPrecision(18, 5);
        builder.Property(t => t.Fees).HasPrecision(18, 4).HasDefaultValue(0m);
        builder.Property(t => t.ManufacturingFeePerGram).HasPrecision(18, 5).HasDefaultValue(0m);
        builder.Property(t => t.NetAmount).HasPrecision(18, 5);
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(t => t.Asset)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.AssetId);
        builder.HasIndex(t => t.TransactionDate);
    }
}
