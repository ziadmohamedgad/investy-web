using Investment.Domain.Entities;
using Investment.Domain.Interfaces;
using Investment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Investment.Infrastructure.Repositories;

public class PriceFetchLogRepository : IPriceFetchLogRepository
{
    private readonly InvestmentDbContext _context;

    public PriceFetchLogRepository(InvestmentDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PriceFetchLog>> GetLatestAsync(int count)
    {
        return await _context.PriceFetchLogs
            .AsNoTracking()
            .OrderByDescending(l => l.FetchDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<PriceFetchLog?> GetLastSuccessfulAsync()
    {
        return await _context.PriceFetchLogs
            .Where(l => l.Success)
            .AsNoTracking()
            .OrderByDescending(l => l.FetchDate)
            .FirstOrDefaultAsync();
    }

    public async Task<PriceFetchLog> AddAsync(PriceFetchLog log)
    {
        _context.PriceFetchLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }
}
