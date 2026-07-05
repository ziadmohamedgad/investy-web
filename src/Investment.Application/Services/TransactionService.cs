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
    private const decimal QuantityTolerance = 0.0000001m;

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
        EnsureNotFutureDate(dto.TransactionDate);

        var asset = await _unitOfWork.Assets.GetByIdAsync(dto.AssetId)
            ?? throw new InvalidOperationException($"Asset {dto.AssetId} not found.");
        var txnType = Enum.Parse<TransactionType>(dto.TransactionType, true);
        var existingTxnsForAsset = (await _unitOfWork.Transactions.GetByAssetIdOrderedAsync(dto.AssetId)).ToList();
        var existingTxns = asset.IsDailyAccrualFund || txnType == TransactionType.Sell
            ? existingTxnsForAsset
            : new List<Transaction>();
        var accrualStartDate = AssetService.GetDailyAccrualStartDate(asset, existingTxns, dto.TransactionDate);
        var dk = Enum.TryParse<DividendKind>(dto.DividendKind, true, out var parsedDk) ? parsedDk : DividendKind.Cash;
        var normalized = NormalizeTransaction(asset, txnType, dto.TransactionDate, dto.Quantity, dto.PricePerUnit, dto.Fees, dto.ManufacturingFeePerGram, accrualStartDate, dk);
        await ValidateSellAgainstCurrentMarketValueAsync(asset, existingTxnsForAsset, txnType, normalized.NetAmount);

        var transaction = new Transaction
        {
            TransactionId = int.MaxValue,
            AssetId = dto.AssetId,
            TransactionType = txnType,
            TransactionDate = dto.TransactionDate,
            Quantity = normalized.Quantity,
            PricePerUnit = normalized.PricePerUnit,
            TotalAmount = normalized.TotalAmount,
            Fees = dto.Fees,
            ManufacturingFeePerGram = dto.ManufacturingFeePerGram,
            NetAmount = normalized.NetAmount,
            DividendKind = dk,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        ValidateTransactionSequence(asset, existingTxnsForAsset.Append(transaction), accrualStartDate);
        transaction.TransactionId = 0;
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
        EnsureNotFutureDate(dto.TransactionDate);

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
        var dk = Enum.TryParse<DividendKind>(dto.DividendKind, true, out var parsedDk) ? parsedDk : DividendKind.Cash;
        var normalized = NormalizeTransaction(asset, txnType, dto.TransactionDate, dto.Quantity, dto.PricePerUnit, dto.Fees, dto.ManufacturingFeePerGram, accrualStartDate, dk);
        await ValidateSellAgainstCurrentMarketValueAsync(asset, existingTxns, txnType, normalized.NetAmount);

        transaction.AssetId = dto.AssetId;
        transaction.TransactionType = txnType;
        transaction.TransactionDate = dto.TransactionDate;
        transaction.Quantity = normalized.Quantity;
        transaction.PricePerUnit = normalized.PricePerUnit;
        transaction.TotalAmount = normalized.TotalAmount;
        transaction.Fees = dto.Fees;
        transaction.ManufacturingFeePerGram = dto.ManufacturingFeePerGram;
        transaction.NetAmount = normalized.NetAmount;
        transaction.DividendKind = dk;
        transaction.Notes = dto.Notes;

        ValidateTransactionSequence(asset, existingTxns.Append(transaction), accrualStartDate);
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
        DateTime accrualStartDate,
        DividendKind dividendKind)
    {
        var isPreciousMetal = asset.AssetType == AssetType.Gold || asset.AssetType == AssetType.Silver;
        var metalPerGramAmount = isPreciousMetal ? quantity * manufacturingFeePerGram : 0m;

        if (txnType == TransactionType.Dividend)
        {
            // quantity = free shares (Stock) or cash amount (Cash)
            // For Stock dividend: NetAmount = 0 (no cash changes hands)
            // For Cash dividend: NetAmount = quantity (the cash received)
            return dividendKind == DividendKind.Stock
                ? (quantity, pricePerUnit, 0m, 0m)
                : (0m, 0m, quantity, quantity);
        }

        if (!asset.IsDailyAccrualFund)
        {
            var totalAmount = quantity * pricePerUnit;
            var netAmount = txnType == TransactionType.Buy
                ? totalAmount + metalPerGramAmount + fees
                : totalAmount + metalPerGramAmount - fees;

            if (txnType == TransactionType.Sell && netAmount < 0)
            {
                throw new InvalidOperationException("صافي البيع بعد الرسوم لا يمكن أن يكون أقل من صفر.");
            }

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

        if (txnType == TransactionType.Sell && net < 0)
        {
            throw new InvalidOperationException("صافي السحب بعد الرسوم لا يمكن أن يكون أقل من صفر.");
        }

        return (units, unitPrice, amount, net);
    }

    private static void EnsureNotFutureDate(DateTime transactionDate)
    {
        if (transactionDate.Date > DateTime.Now.Date)
        {
            throw new InvalidOperationException("لا يمكن تسجيل عملية بتاريخ مستقبلي.");
        }
    }

    private async Task ValidateSellAgainstCurrentMarketValueAsync(
        Asset asset,
        List<Transaction> existingTransactions,
        TransactionType txnType,
        decimal netAmount)
    {
        if (txnType != TransactionType.Sell || netAmount <= 0)
        {
            return;
        }

        var latestPrice = await _unitOfWork.Prices.GetLatestByAssetIdAsync(asset.AssetId);
        var currentPrice = latestPrice?.PriceValue ?? 0m;
        var currentSummary = AssetService.CalculateAssetSummary(asset, existingTransactions, currentPrice);
        if (netAmount > currentSummary.CurrentValue + 0.01m)
        {
            throw new InvalidOperationException("صافي البيع بعد الرسوم يجب ألا يتخطى القيمة السوقية الحالية للأصل.");
        }
    }

    private static void ValidateTransactionSequence(Asset asset, IEnumerable<Transaction> transactions, DateTime accrualStartDate)
    {
        decimal unitsHeld = 0;
        var hasBuy = false;

        foreach (var transaction in transactions.OrderBy(t => t.TransactionDate).ThenBy(t => t.TransactionId))
        {
            var quantity = GetEffectiveQuantity(asset, transaction, accrualStartDate);

            if (transaction.TransactionType == TransactionType.Buy)
            {
                hasBuy = true;
                unitsHeld += quantity;
                continue;
            }

            if (transaction.TransactionType == TransactionType.Dividend)
            {
                if (!hasBuy)
                    throw new InvalidOperationException("لا يمكن تسجيل أرباح قبل وجود عملية شراء سابقة لهذا الأصل.");

                if (transaction.DividendKind == DividendKind.Stock)
                    unitsHeld += transaction.Quantity;

                continue;
            }

            if (!hasBuy)
            {
                throw new InvalidOperationException(asset.IsDailyAccrualFund
                    ? "لا يمكن تسجيل سحب قبل وجود إيداع سابق لهذا الأصل."
                    : "لا يمكن تسجيل بيع قبل وجود عملية شراء سابقة لهذا الأصل.");
            }

            if (quantity > unitsHeld + QuantityTolerance)
            {
                throw new InvalidOperationException(asset.IsDailyAccrualFund
                    ? "لا يمكن إتمام هذه العملية لأن مبلغ السحب يتجاوز المبلغ المتاح من هذا الأصل."
                    : "لا يمكن إتمام هذه العملية لأن الكمية المباعة تتجاوز الكمية المتاحة من هذا الأصل.");
            }

            unitsHeld -= quantity;
        }
    }

    private static decimal GetEffectiveQuantity(Asset asset, Transaction transaction, DateTime accrualStartDate)
    {
        if (!asset.IsDailyAccrualFund)
        {
            return transaction.Quantity;
        }

        var unitPrice = AssetService.GetDailyAccrualUnitPrice(asset, transaction.TransactionDate, accrualStartDate);
        return unitPrice > 0 ? transaction.TotalAmount / unitPrice : 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
        if (transaction == null) return false;

        var asset = await _unitOfWork.Assets.GetByIdAsync(transaction.AssetId);
        var transactionsBeforeDelete = (await _unitOfWork.Transactions.GetByAssetIdOrderedAsync(transaction.AssetId)).ToList();
        var remainingBeforeDelete = (await _unitOfWork.Transactions.GetByAssetIdOrderedAsync(transaction.AssetId))
            .Where(t => t.TransactionId != id)
            .ToList();

        if (asset != null && remainingBeforeDelete.Any())
        {
            var accrualStartDate = AssetService.GetDailyAccrualStartDate(asset, remainingBeforeDelete);
            ValidateTransactionSequence(asset, remainingBeforeDelete, accrualStartDate);
        }

        await _unitOfWork.Transactions.DeleteAsync(id);

        var remaining = (await _unitOfWork.Transactions.GetByAssetIdAsync(transaction.AssetId)).ToList();

        if (!remaining.Any())
        {
            var latestPrice = await _unitOfWork.Prices.GetLatestByAssetIdAsync(transaction.AssetId);
            var closingSummary = asset == null
                ? null
                : AssetService.CalculateAssetSummary(asset, transactionsBeforeDelete, latestPrice?.PriceValue ?? 0m);
            var realizedPnL = closingSummary?.RealizedPnL ?? 0m;

            if (Math.Abs(realizedPnL) <= 0.005m)
            {
                await _unitOfWork.Assets.DeleteAsync(transaction.AssetId);
            }
            else if (asset != null)
            {
                asset.ClosedRealizedPnL = realizedPnL;
                await _unitOfWork.Assets.UpdateAsync(asset);
            }
        }

        await _excelSyncService.RefreshAsync();

        return true;
    }
}
