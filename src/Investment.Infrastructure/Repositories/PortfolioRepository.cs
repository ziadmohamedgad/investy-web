using Investment.Domain.Entities;
using Investment.Domain.Interfaces;
using Investment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Investment.Infrastructure.Repositories;

public class PortfolioRepository : IPortfolioRepository
{
    private readonly InvestmentDbContext _context;

    public PortfolioRepository(InvestmentDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Portfolio>> GetAllAsync()
    {
        return await _context.Portfolios
            .Include(p => p.PortfolioAssets)
            .ThenInclude(pa => pa.Asset)
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Portfolio?> GetByIdAsync(int id)
    {
        return await _context.Portfolios.FindAsync(id);
    }

    public async Task<Portfolio?> GetByIdWithAssetsAsync(int id)
    {
        return await _context.Portfolios
            .Include(p => p.PortfolioAssets)
            .ThenInclude(pa => pa.Asset)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PortfolioId == id);
    }

    public async Task<Portfolio> AddAsync(Portfolio portfolio)
    {
        _context.Portfolios.Add(portfolio);
        await _context.SaveChangesAsync();
        return portfolio;
    }

    public async Task UpdateAsync(Portfolio portfolio)
    {
        _context.Portfolios.Update(portfolio);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var portfolio = await _context.Portfolios.FindAsync(id);
        if (portfolio != null)
        {
            _context.Portfolios.Remove(portfolio);
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddAssetToPortfolioAsync(int portfolioId, int assetId)
    {
        var exists = await _context.PortfolioAssets
            .AnyAsync(pa => pa.PortfolioId == portfolioId && pa.AssetId == assetId);

        if (!exists)
        {
            _context.PortfolioAssets.Add(new PortfolioAsset
            {
                PortfolioId = portfolioId,
                AssetId = assetId
            });
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveAssetFromPortfolioAsync(int portfolioId, int assetId)
    {
        var pa = await _context.PortfolioAssets
            .FirstOrDefaultAsync(pa => pa.PortfolioId == portfolioId && pa.AssetId == assetId);
        if (pa != null)
        {
            _context.PortfolioAssets.Remove(pa);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<int>> GetAssetIdsByPortfolioIdAsync(int portfolioId)
    {
        return await _context.PortfolioAssets
            .Where(pa => pa.PortfolioId == portfolioId)
            .Select(pa => pa.AssetId)
            .ToListAsync();
    }
}
