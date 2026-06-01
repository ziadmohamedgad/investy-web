using Investment.Domain.Entities;
using Investment.Domain.Enums;
using Investment.Domain.Interfaces;
using Investment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Investment.Infrastructure.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly InvestmentDbContext _context;

    public AssetRepository(InvestmentDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Asset>> GetAllAsync()
    {
        return await _context.Assets
            .Include(a => a.PortfolioAssets)
            .ThenInclude(pa => pa.Portfolio)
            .AsNoTracking()
            .OrderBy(a => a.AssetName)
            .ToListAsync();
    }

    public async Task<Asset?> GetByIdAsync(int id)
    {
        return await _context.Assets
            .Include(a => a.PortfolioAssets)
            .ThenInclude(pa => pa.Portfolio)
            .FirstOrDefaultAsync(a => a.AssetId == id);
    }

    public async Task<Asset?> GetByCodeAsync(string code)
    {
        return await _context.Assets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssetCode.ToLower() == code.ToLower().Trim());
    }

    public async Task<Asset> AddAsync(Asset asset)
    {
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();
        return asset;
    }

    public async Task UpdateAsync(Asset asset)
    {
        _context.Assets.Update(asset);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset != null)
        {
            _context.Assets.Remove(asset);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> CountActiveAssetsAsync()
    {
        return await _context.Assets.CountAsync(a => a.IsActive);
    }

    public async Task<IEnumerable<Asset>> GetActiveAssetsWithTickerAsync()
    {
        return await _context.Assets
            .Where(a => a.IsActive && a.ExternalTicker != null && a.ExternalTicker != "")
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Asset>> GetActiveStockAssetsWithTickerAsync()
    {
        return await _context.Assets
            .Where(a => a.IsActive
                        && a.AssetType == AssetType.Stock
                        && a.ExternalTicker != null
                        && a.ExternalTicker != "")
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> CountActiveStockAssetsWithTickerAsync()
    {
        return await _context.Assets
            .CountAsync(a => a.IsActive
                             && a.AssetType == AssetType.Stock
                             && a.ExternalTicker != null
                             && a.ExternalTicker != "");
    }

    public async Task<IEnumerable<Asset>> GetActiveAssetsAsync()
    {
        return await _context.Assets
            .Where(a => a.IsActive)
            .AsNoTracking()
            .ToListAsync();
    }
}
