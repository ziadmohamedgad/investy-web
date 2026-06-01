using Investment.Domain.Entities;

namespace Investment.Domain.Interfaces;

public interface IPriceRepository
{
    Task<IEnumerable<Price>> GetAllAsync();
    Task<Price?> GetByIdAsync(int id);
    Task<IEnumerable<Price>> GetByAssetIdAsync(int assetId);
    Task<Price?> GetLatestByAssetIdAsync(int assetId);
    Task<Price?> GetByAssetIdAndDateAsync(int assetId, DateTime date);
    Task<IEnumerable<Price>> GetByAssetIdAndDateRangeAsync(int assetId, DateTime fromDate, DateTime toDate);
    Task<Price> AddAsync(Price price);
    Task AddRangeAsync(IEnumerable<Price> prices);
    Task<Dictionary<int, Price>> GetLatestPricesForAssetsAsync(IEnumerable<int> assetIds);
    Task<Price?> GetLastPriceForAssetOnDateAsync(int assetId, DateTime date);
    Task<Price?> GetPriceBeforeOrOnDateAsync(int assetId, DateTime date);
}
