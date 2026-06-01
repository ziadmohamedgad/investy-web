using Investment.Domain.Entities;

namespace Investment.Domain.Interfaces;

public interface IAssetRepository
{
    Task<IEnumerable<Asset>> GetAllAsync();
    Task<Asset?> GetByIdAsync(int id);
    Task<Asset?> GetByCodeAsync(string code);
    Task<Asset> AddAsync(Asset asset);
    Task UpdateAsync(Asset asset);
    Task DeleteAsync(int id);
    Task<int> CountActiveAssetsAsync();
    Task<IEnumerable<Asset>> GetActiveAssetsWithTickerAsync();
    Task<IEnumerable<Asset>> GetActiveStockAssetsWithTickerAsync();
    Task<int> CountActiveStockAssetsWithTickerAsync();
    Task<IEnumerable<Asset>> GetActiveAssetsAsync();
}
