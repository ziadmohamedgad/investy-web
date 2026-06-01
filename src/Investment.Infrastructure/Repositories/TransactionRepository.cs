using Investment.Domain.Entities;
using Investment.Domain.Enums;
using Investment.Domain.Interfaces;
using Investment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Investment.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly InvestmentDbContext _context;

    public TransactionRepository(InvestmentDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Transaction>> GetAllAsync()
    {
        return await _context.Transactions
            .Include(t => t.Asset)
            .AsNoTracking()
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<Transaction?> GetByIdAsync(int id)
    {
        return await _context.Transactions
            .Include(t => t.Asset)
            .FirstOrDefaultAsync(t => t.TransactionId == id);
    }

    public async Task<IEnumerable<Transaction>> GetByAssetIdAsync(int assetId)
    {
        return await _context.Transactions
            .Where(t => t.AssetId == assetId)
            .AsNoTracking()
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetFilteredAsync(int? assetId, TransactionType? type, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Transactions.Include(t => t.Asset).AsNoTracking().AsQueryable();

        if (assetId.HasValue)
            query = query.Where(t => t.AssetId == assetId.Value);
        if (type.HasValue)
            query = query.Where(t => t.TransactionType == type.Value);
        if (fromDate.HasValue)
            query = query.Where(t => t.TransactionDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(t => t.TransactionDate <= toDate.Value);

        return await query.OrderByDescending(t => t.TransactionDate).ToListAsync();
    }

    public async Task<Transaction> AddAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction != null)
        {
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Transaction>> GetByAssetIdOrderedAsync(int assetId)
    {
        return await _context.Transactions
            .Where(t => t.AssetId == assetId)
            .AsNoTracking()
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.TransactionId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByAssetIdsOrderedAsync(IEnumerable<int> assetIds)
    {
        var idList = assetIds.ToList();
        if (idList.Count == 0)
        {
            return Enumerable.Empty<Transaction>();
        }

        return await _context.Transactions
            .Where(t => idList.Contains(t.AssetId))
            .AsNoTracking()
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.TransactionId)
            .ToListAsync();
    }
}
