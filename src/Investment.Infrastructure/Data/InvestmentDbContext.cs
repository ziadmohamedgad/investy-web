using Investment.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Investment.Infrastructure.Data;

public class InvestmentDbContext : DbContext
{
    public InvestmentDbContext(DbContextOptions<InvestmentDbContext> options) : base(options) { }

    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Price> Prices => Set<Price>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<PortfolioAsset> PortfolioAssets => Set<PortfolioAsset>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<PriceFetchLog> PriceFetchLogs => Set<PriceFetchLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvestmentDbContext).Assembly);
    }
}
