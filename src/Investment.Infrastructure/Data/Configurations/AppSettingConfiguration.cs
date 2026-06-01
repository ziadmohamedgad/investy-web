using Investment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Investment.Infrastructure.Data.Configurations;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.HasKey(s => s.SettingKey);
        builder.Property(s => s.SettingKey).HasMaxLength(100);
        builder.Property(s => s.SettingValue).HasMaxLength(500);
        builder.Property(s => s.LastUpdated).HasDefaultValueSql("GETUTCDATE()");
    }
}
