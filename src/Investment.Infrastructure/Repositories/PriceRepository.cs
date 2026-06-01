using Investment.Domain.Entities;
using Investment.Domain.Interfaces;
using Investment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Investment.Infrastructure.Repositories;

public class PriceRepository : IPriceRepository
{
    private readonly InvestmentDbContext _context;

    public PriceRepository(InvestmentDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Price>> GetAllAsync()
    {
        return await _context.Prices
            .Include(p => p.Asset)
            .AsNoTracking()
            .OrderByDescending(p => p.PriceDate)
            .ToListAsync();
    }

    public async Task<Price?> GetByIdAsync(int id)
    {
        return await _context.Prices
            .Include(p => p.Asset)
            .FirstOrDefaultAsync(p => p.PriceId == id);
    }

    public async Task<IEnumerable<Price>> GetByAssetIdAsync(int assetId)
    {
        return await _context.Prices
            .Where(p => p.AssetId == assetId)
            .AsNoTracking()
            .OrderByDescending(p => p.PriceDate)
            .ToListAsync();
    }

    public async Task<Price?> GetLatestByAssetIdAsync(int assetId)
    {
        return await _context.Prices
            .Where(p => p.AssetId == assetId)
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.PriceId)
            .FirstOrDefaultAsync();
    }

    public async Task<Price?> GetByAssetIdAndDateAsync(int assetId, DateTime date)
    {
        return await _context.Prices
            .Where(p => p.AssetId == assetId && p.PriceDate.Date == date.Date)
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Price>> GetByAssetIdAndDateRangeAsync(int assetId, DateTime fromDate, DateTime toDate)
    {
        return await _context.Prices
            .Where(p => p.AssetId == assetId && p.PriceDate >= fromDate && p.PriceDate <= toDate)
            .AsNoTracking()
            .OrderBy(p => p.PriceDate)
            .ToListAsync();
    }

    public async Task<Price> AddAsync(Price price)
    {
        var existing = await _context.Prices
            .FirstOrDefaultAsync(p => p.AssetId == price.AssetId);

        if (existing != null)
        {
            existing.PriceValue = price.PriceValue;
            existing.Source = price.Source;
            existing.PriceDate = price.PriceDate;
            existing.CreatedAt = DateTime.UtcNow;
            _context.Prices.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        _context.Prices.Add(price);
        await _context.SaveChangesAsync();
        return price;
    }

    public async Task AddRangeAsync(IEnumerable<Price> prices)
    {
        foreach (var price in prices)
        {
            await AddAsync(price);
        }
    }

    public async Task<Dictionary<int, Price>> GetLatestPricesForAssetsAsync(IEnumerable<int> assetIds)
    {
        var assetIdList = assetIds.ToList();
        var latestPrices = await _context.Prices
            .Where(p => assetIdList.Contains(p.AssetId))
            .AsNoTracking()
            .GroupBy(p => p.AssetId)
            .Select(g => g.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.PriceId).First())
            .ToListAsync();

        return latestPrices.ToDictionary(p => p.AssetId);
    }

    public async Task<Price?> GetLastPriceForAssetOnDateAsync(int assetId, DateTime date)
    {
        return await _context.Prices
            .Where(p => p.AssetId == assetId && p.PriceDate.Date == date.Date)
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<Price?> GetPriceBeforeOrOnDateAsync(int assetId, DateTime date)
    {
        return await _context.Prices
            .Where(p => p.AssetId == assetId && p.PriceDate.Date <= date.Date)
            .AsNoTracking()
            .OrderByDescending(p => p.PriceDate)
            .ThenByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
