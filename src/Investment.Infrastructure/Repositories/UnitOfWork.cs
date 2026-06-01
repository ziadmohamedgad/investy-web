using Investment.Domain.Interfaces;
using Investment.Infrastructure.Data;

namespace Investment.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly InvestmentDbContext _context;
    private IAssetRepository? _assets;
    private ITransactionRepository? _transactions;
    private IPriceRepository? _prices;
    private IPortfolioRepository? _portfolios;
    private IAppSettingRepository? _appSettings;
    private IPriceFetchLogRepository? _priceFetchLogs;

    public UnitOfWork(InvestmentDbContext context)
    {
        _context = context;
    }

    public IAssetRepository Assets => _assets ??= new AssetRepository(_context);
    public ITransactionRepository Transactions => _transactions ??= new TransactionRepository(_context);
    public IPriceRepository Prices => _prices ??= new PriceRepository(_context);
    public IPortfolioRepository Portfolios => _portfolios ??= new PortfolioRepository(_context);
    public IAppSettingRepository AppSettings => _appSettings ??= new AppSettingRepository(_context);
    public IPriceFetchLogRepository PriceFetchLogs => _priceFetchLogs ??= new PriceFetchLogRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
