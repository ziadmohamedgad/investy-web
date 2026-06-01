namespace Investment.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IAssetRepository Assets { get; }
    ITransactionRepository Transactions { get; }
    IPriceRepository Prices { get; }
    IPortfolioRepository Portfolios { get; }
    IAppSettingRepository AppSettings { get; }
    IPriceFetchLogRepository PriceFetchLogs { get; }
    Task<int> SaveChangesAsync();
}
