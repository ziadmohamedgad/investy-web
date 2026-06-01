using Investment.Domain.Entities;
using Investment.Domain.Enums;

namespace Investment.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetAllAsync();
    Task<Transaction?> GetByIdAsync(int id);
    Task<IEnumerable<Transaction>> GetByAssetIdAsync(int assetId);
    Task<IEnumerable<Transaction>> GetFilteredAsync(int? assetId, TransactionType? type, DateTime? fromDate, DateTime? toDate);
    Task<Transaction> AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task DeleteAsync(int id);
    Task<IEnumerable<Transaction>> GetByAssetIdOrderedAsync(int assetId);
    Task<IEnumerable<Transaction>> GetByAssetIdsOrderedAsync(IEnumerable<int> assetIds);
}
