using AutoMapper;
using Investment.Application.DTOs;
using Investment.Domain.Entities;
using Investment.Domain.Enums;
using Investment.Domain.Interfaces;

namespace Investment.Application.Services;

public interface ITransactionService
{
    Task<IEnumerable<TransactionDto>> GetAllAsync();
    Task<TransactionDto?> GetByIdAsync(int id);
    Task<IEnumerable<TransactionDto>> GetFilteredAsync(int? assetId, string? type, DateTime? fromDate, DateTime? toDate);
    Task<TransactionDto> CreateAsync(CreateTransactionDto dto);
    Task<TransactionDto?> UpdateAsync(int id, UpdateTransactionDto dto);
    Task<bool> DeleteAsync(int id);
}

public class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IExcelSyncService _excelSyncService;

    public TransactionService(IUnitOfWork unitOfWork, IMapper mapper, IExcelSyncService excelSyncService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _excelSyncService = excelSyncService;
    }

    public async Task<IEnumerable<TransactionDto>> GetAllAsync()
    {
        var transactions = await _unitOfWork.Transactions.GetAllAsync();
        return _mapper.Map<IEnumerable<TransactionDto>>(transactions);
    }

    public async Task<TransactionDto?> GetByIdAsync(int id)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
        return transaction == null ? null : _mapper.Map<TransactionDto>(transaction);
    }

    public async Task<IEnumerable<TransactionDto>> GetFilteredAsync(int? assetId, string? type, DateTime? fromDate, DateTime? toDate)
    {
        TransactionType? txnType = null;
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<TransactionType>(type, true, out var parsed))
            txnType = parsed;

        var transactions = await _unitOfWork.Transactions.GetFilteredAsync(assetId, txnType, fromDate, toDate);
        return _mapper.Map<IEnumerable<TransactionDto>>(transactions);
    }

    public async Task<TransactionDto> CreateAsync(CreateTransactionDto dto)
    {
        var asset = await _unitOfWork.Assets.GetByIdAsync(dto.AssetId)
            ?? throw new InvalidOperationException($"Asset {dto.AssetId} not found.");
        var txnType = Enum.Parse<TransactionType>(dto.TransactionType, true);
        var existingTxnsForAsset = (await _unitOfWork.Transactions.GetByAssetIdOrderedAsync(dto.AssetId)).ToList();
        var existingTxns = asset.IsDailyAccrualFund || txnType == TransactionType.Sell
            ? existingTxnsForAsset
            : new List<Transaction>();
        var accrualStartDate = AssetService.GetDailyAccrualStartDate(asset, existingTxns, dto.TransactionDate);
        var normalized = NormalizeTransaction(asset, txnType, dto.TransactionDate, dto.Quantity, dto.PricePerUnit, dto.Fees, dto.ManufacturingFeePerGram, accrualStartDate);

        // Validate sell doesn't exceed held units
        if (txnType == TransactionType.Sell)
        {
            var unitsHeld = CalculateUnitsHeld(asset, existingTxns, accrualStartDate);

            if (normalized.Quantity > unitsHeld)
                throw new InvalidOperationException($"Cannot sell {normalized.Quantity} units. Only {unitsHeld} units held.");
        }

        var transaction = new Transaction
        {
            AssetId = dto.AssetId,
            TransactionType = txnType,
            TransactionDate = dto.TransactionDate,
            Quantity = normalized.Quantity,
            PricePerUnit = normalized.PricePerUnit,
            TotalAmount = normalized.TotalAmount,
            Fees = dto.Fees,
            ManufacturingFeePerGram = dto.ManufacturingFeePerGram,
            NetAmount = normalized.NetAmount,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _unitOfWork.Transactions.AddAsync(transaction);

        await UpdateExistingStockPriceFromTransactionIfNewerAsync(asset, existingTxnsForAsset, dto.TransactionDate, normalized.PricePerUnit);

        await _excelSyncService.RefreshAsync();

        // Reload with asset info for mapping
        var result = await _unitOfWork.Transactions.GetByIdAsync(created.TransactionId);
        return _mapper.Map<TransactionDto>(result);
    }

    private async Task UpdateExistingStockPriceFromTransactionIfNewerAsync(
        Asset asset,
        List<Transaction> existingTxnsForAsset,
        DateTime transactionDate,
        decimal pricePerUnit)
    {
        if (asset.AssetType != AssetType.Stock || existingTxnsForAsset.Count == 0 || pricePerUnit <= 0)
        {
            return;
        }

        var latestPrice = await _unitOfWork.Prices.GetLatestByAssetIdAsync(asset.AssetId);
        if (latestPrice != null && latestPrice.PriceDate.Date > transactionDate.Date)
        {
            return;
        }

        await _unitOfWork.Prices.AddAsync(new Price
        {
            AssetId = asset.AssetId,
            PriceDate = transactionDate.Date,
            PriceValue = pricePerUnit,
            Source = PriceSource.Manual,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task<TransactionDto?> UpdateAsync(int id, UpdateTransactionDto dto)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
        if (transaction == null) return null;

        if (dto.AssetId != transaction.AssetId)
        {
            throw new InvalidOperationException("Cannot change the asset while editing an existing transaction.");
        }

        var asset = await _unitOfWork.Assets.GetByIdAsync(dto.AssetId)
            ?? throw new InvalidOperationException($"Asset {dto.AssetId} not found.");
        var txnType = Enum.Parse<TransactionType>(dto.TransactionType, true);
        var existingTxns = (await _unitOfWork.Transactions.GetByAssetIdOrderedAsync(dto.AssetId))
            .Where(t => t.TransactionId != id)
            .ToList();
        var accrualStartDate = AssetService.GetDailyAccrualStartDate(asset, existingTxns, dto.TransactionDate);
        var normalized = NormalizeTransaction(asset, txnType, dto.TransactionDate, dto.Quantity, dto.PricePerUnit, dto.Fees, dto.ManufacturingFeePerGram, accrualStartDate);

        if (txnType == TransactionType.Sell)
        {
            var unitsHeld = CalculateUnitsHeld(asset, existingTxns, accrualStartDate);

            if (normalized.Quantity > unitsHeld)
                throw new InvalidOperationException($"Cannot sell {normalized.Quantity} units. Only {unitsHeld} units held.");
        }

        transaction.AssetId = dto.AssetId;
        transaction.TransactionType = txnType;
        transaction.TransactionDate = dto.TransactionDate;
        transaction.Quantity = normalized.Quantity;
        transaction.PricePerUnit = normalized.PricePerUnit;
        transaction.TotalAmount = normalized.TotalAmount;
        transaction.Fees = dto.Fees;
        transaction.ManufacturingFeePerGram = dto.ManufacturingFeePerGram;
        transaction.NetAmount = normalized.NetAmount;
        transaction.Notes = dto.Notes;

        await _unitOfWork.Transactions.UpdateAsync(transaction);

        await _excelSyncService.RefreshAsync();

        var result = await _unitOfWork.Transactions.GetByIdAsync(id);
        return _mapper.Map<TransactionDto>(result);
    }

    private static (decimal Quantity, decimal PricePerUnit, decimal TotalAmount, decimal NetAmount) NormalizeTransaction(
        Investment.Domain.Entities.Asset asset,
        TransactionType txnType,
        DateTime transactionDate,
        decimal quantity,
        decimal pricePerUnit,
        decimal fees,
        decimal manufacturingFeePerGram,
        DateTime accrualStartDate)
    {
        var goldPerGramAmount = asset.AssetType == AssetType.Gold
            ? quantity * manufacturingFeePerGram
            : 0m;

        if (!asset.IsDailyAccrualFund)
        {
            var totalAmount = quantity * pricePerUnit;
            var netAmount = txnType == TransactionType.Buy
                ? totalAmount + goldPerGramAmount + fees
                : totalAmount + goldPerGramAmount - fees;

            return (quantity, pricePerUnit, totalAmount, netAmount);
        }

        var unitPrice = AssetService.GetDailyAccrualUnitPrice(asset, transactionDate, accrualStartDate);
        if (unitPrice <= 0)
        {
            throw new InvalidOperationException("Unable to calculate the fund unit price.");
        }

        var amount = quantity;
        var units = amount / unitPrice;
        var net = txnType == TransactionType.Buy
            ? amount + fees
            : amount - fees;

        return (units, unitPrice, amount, net);
    }

    private static decimal CalculateUnitsHeld(Asset asset, IEnumerable<Transaction> transactions, DateTime accrualStartDate)
    {
        decimal unitsHeld = 0;

        foreach (var transaction in transactions)
        {
            var quantity = transaction.Quantity;
            if (asset.IsDailyAccrualFund)
            {
                var unitPrice = AssetService.GetDailyAccrualUnitPrice(asset, transaction.TransactionDate, accrualStartDate);
                quantity = unitPrice > 0 ? transaction.TotalAmount / unitPrice : 0;
            }

            unitsHeld += transaction.TransactionType == TransactionType.Buy ? quantity : -quantity;
        }

        return unitsHeld;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
        if (transaction == null) return false;

        var asset = await _unitOfWork.Assets.GetByIdAsync(transaction.AssetId);

        await _unitOfWork.Transactions.DeleteAsync(id);

        var remaining = (await _unitOfWork.Transactions.GetByAssetIdAsync(transaction.AssetId)).ToList();

        if (!remaining.Any())
        {
            // Hard-delete the asset (cascade will remove portfolio links)
            await _unitOfWork.Assets.DeleteAsync(transaction.AssetId);
        }

        await _excelSyncService.RefreshAsync();

        return true;
    }
}
