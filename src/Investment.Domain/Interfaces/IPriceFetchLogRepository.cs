using Investment.Domain.Entities;

namespace Investment.Domain.Interfaces;

public interface IPriceFetchLogRepository
{
    Task<IEnumerable<PriceFetchLog>> GetLatestAsync(int count);
    Task<PriceFetchLog?> GetLastSuccessfulAsync();
    Task<PriceFetchLog> AddAsync(PriceFetchLog log);
}
