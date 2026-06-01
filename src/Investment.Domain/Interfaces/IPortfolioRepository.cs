using Investment.Domain.Entities;

namespace Investment.Domain.Interfaces;

public interface IPortfolioRepository
{
    Task<IEnumerable<Portfolio>> GetAllAsync();
    Task<Portfolio?> GetByIdAsync(int id);
    Task<Portfolio?> GetByIdWithAssetsAsync(int id);
    Task<Portfolio> AddAsync(Portfolio portfolio);
    Task UpdateAsync(Portfolio portfolio);
    Task DeleteAsync(int id);
    Task AddAssetToPortfolioAsync(int portfolioId, int assetId);
    Task RemoveAssetFromPortfolioAsync(int portfolioId, int assetId);
    Task<IEnumerable<int>> GetAssetIdsByPortfolioIdAsync(int portfolioId);
}
