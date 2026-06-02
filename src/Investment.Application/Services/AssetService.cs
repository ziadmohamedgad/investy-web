using AutoMapper;
using Investment.Application.DTOs;
using Investment.Domain.Entities;
using Investment.Domain.Enums;
using Investment.Domain.Interfaces;

namespace Investment.Application.Services;

public interface IAssetService
{
    Task<IEnumerable<AssetDto>> GetAllAsync();
    Task<AssetDto?> GetByIdAsync(int id);
    Task<AssetDto?> GetByCodeAsync(string code);
    Task<AssetSummaryDto?> GetSummaryAsync(int id);
    Task<AssetDto> CreateAsync(CreateAssetDto dto);
    Task<AssetDto> CreateManualAsync(CreateManualAssetDto dto);
    Task<bool> SetCurrentPriceAsync(int assetId, decimal price, DateTime? priceDate = null);
    Task<bool> UpdateFinancialSettingsAsync(int assetId, SetAssetFinancialSettingsDto dto);
    Task<AssetDto?> UpdateAsync(int id, UpdateAssetDto dto);
    Task<bool> DeleteAsync(int id);
}

public class AssetService : IAssetService
{
    private const decimal QuantityTolerance = 0.0000001m;
    private const decimal ClosedPositionQuantityTolerance = 0.005m;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IExcelSyncService _excelSyncService;

    public AssetService(IUnitOfWork unitOfWork, IMapper mapper, IExcelSyncService excelSyncService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _excelSyncService = excelSyncService;
    }

    public async Task<IEnumerable<AssetDto>> GetAllAsync()
    {
        var assets = await _unitOfWork.Assets.GetAllAsync();
        return _mapper.Map<IEnumerable<AssetDto>>(assets);
    }

    public async Task<AssetDto?> GetByIdAsync(int id)
    {
        var asset = await _unitOfWork.Assets.GetByIdAsync(id);
        return asset == null ? null : _mapper.Map<AssetDto>(asset);
    }

    public async Task<AssetDto?> GetByCodeAsync(string code)
    {
        var asset = await _unitOfWork.Assets.GetByCodeAsync(code);
        return asset == null ? null : _mapper.Map<AssetDto>(asset);
    }

    public async Task<AssetSummaryDto?> GetSummaryAsync(int id)
    {
        var asset = await _unitOfWork.Assets.GetByIdAsync(id);
        if (asset == null) return null;

        var transactions = (await _unitOfWork.Transactions.GetByAssetIdOrderedAsync(id)).ToList();
        var latestPrice = await _unitOfWork.Prices.GetLatestByAssetIdAsync(id);

        return CalculateAssetSummary(asset, transactions, latestPrice?.PriceValue ?? 0);
    }

    public async Task<AssetDto> CreateAsync(CreateAssetDto dto)
    {
        var normalizedCode = NormalizeAssetCode(dto.AssetCode);
        if (await _unitOfWork.Assets.GetByCodeAsync(normalizedCode) != null)
            throw new InvalidOperationException($"Asset code '{normalizedCode}' already exists.");

        var asset = _mapper.Map<Asset>(dto);
        asset.AssetCode = normalizedCode;
        asset.AssetName = NormalizeAssetName(dto.AssetName);
        asset.CreatedAt = DateTime.UtcNow;
        asset.IsDailyAccrualFund = dto.IsDailyAccrualFund;
        asset.DailyAccrualAnnualRatePercent = dto.IsDailyAccrualFund && dto.DailyAccrualAnnualRatePercent > 0
            ? dto.DailyAccrualAnnualRatePercent
            : 0m;
        asset.GoldCashbackPerGram = dto.GoldCashbackPerGram >= 0 ? dto.GoldCashbackPerGram : 28.5m;
        var created = await _unitOfWork.Assets.AddAsync(asset);
        await _excelSyncService.RefreshAsync();
        return _mapper.Map<AssetDto>(created);
    }

    public async Task<AssetDto> CreateManualAsync(CreateManualAssetDto dto)
    {
        var normalizedCode = NormalizeAssetCode(dto.AssetCode);
        var existing = await _unitOfWork.Assets.GetByCodeAsync(normalizedCode);

        // If the asset already exists, update its current price and return it
        if (existing != null)
        {
            if (dto.InitialPrice is > 0)
            {
                await _unitOfWork.Prices.AddAsync(new Price
                {
                    AssetId = existing.AssetId,
                    PriceDate = DateTime.UtcNow.Date,
                    PriceValue = dto.InitialPrice.Value,
                    Source = PriceSource.Manual,
                    CreatedAt = DateTime.UtcNow
                });

                await _excelSyncService.RefreshAsync();
            }

            return ToAssetDto(existing);
        }

        if (!Enum.TryParse<AssetType>(dto.AssetType, true, out var assetType) || assetType == AssetType.Stock)
            throw new InvalidOperationException("Manual assets must use Gold, Fund, or Other type.");

        var asset = new Asset
        {
            AssetCode = normalizedCode,
            AssetName = NormalizeAssetName(dto.AssetName),
            AssetType = assetType,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "EGP" : dto.Currency.Trim(),
            ExternalTicker = null,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            IsDailyAccrualFund = dto.IsDailyAccrualFund,
            DailyAccrualAnnualRatePercent = dto.IsDailyAccrualFund && dto.DailyAccrualAnnualRatePercent > 0
                ? dto.DailyAccrualAnnualRatePercent
                : 0m,
            GoldCashbackPerGram = dto.GoldCashbackPerGram >= 0 ? dto.GoldCashbackPerGram : 28.5m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _unitOfWork.Assets.AddAsync(asset);

        if (dto.InitialPrice is > 0)
        {
            await _unitOfWork.Prices.AddAsync(new Price
            {
                AssetId = created.AssetId,
                PriceDate = DateTime.UtcNow.Date,
                PriceValue = dto.InitialPrice.Value,
                Source = PriceSource.Manual,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _excelSyncService.RefreshAsync();

        return ToAssetDto(created);
    }

    private static AssetDto ToAssetDto(Asset asset)
    {
        return new AssetDto
        {
            AssetId = asset.AssetId,
            AssetCode = asset.AssetCode,
            AssetName = asset.AssetName,
            AssetType = asset.AssetType.ToString(),
            Currency = asset.Currency,
            ExternalTicker = asset.ExternalTicker,
            Notes = asset.Notes,
            IsDailyAccrualFund = asset.IsDailyAccrualFund,
            DailyAccrualAnnualRatePercent = asset.DailyAccrualAnnualRatePercent,
            GoldCashbackPerGram = asset.GoldCashbackPerGram,
            IsActive = asset.IsActive,
            CreatedAt = asset.CreatedAt,
            Portfolios = new List<string>()
        };
    }

    public async Task<bool> SetCurrentPriceAsync(int assetId, decimal price, DateTime? priceDate = null)
    {
        if (price < 0)
            throw new InvalidOperationException("السعر لا يمكن أن يكون سالبًا.");

        var asset = await _unitOfWork.Assets.GetByIdAsync(assetId);
        if (asset == null)
            return false;

        await _unitOfWork.Prices.AddAsync(new Price
        {
            AssetId = assetId,
            PriceDate = (priceDate ?? DateTime.UtcNow).Date,
            PriceValue = price,
            Source = PriceSource.Manual,
            CreatedAt = DateTime.UtcNow
        });

        await _excelSyncService.RefreshAsync();

        return true;
    }

    public async Task<bool> UpdateFinancialSettingsAsync(int assetId, SetAssetFinancialSettingsDto dto)
    {
        var asset = await _unitOfWork.Assets.GetByIdAsync(assetId);
        if (asset == null)
            return false;

        if (asset.AssetType == AssetType.Gold && dto.GoldCashbackPerGram.HasValue)
        {
            if (dto.GoldCashbackPerGram.Value < 0)
                throw new InvalidOperationException("Gold cashback per gram cannot be negative.");

            asset.GoldCashbackPerGram = dto.GoldCashbackPerGram.Value;
        }

        if (asset.IsDailyAccrualFund && dto.DailyAccrualAnnualRatePercent.HasValue)
        {
            if (dto.DailyAccrualAnnualRatePercent.Value <= 0)
                throw new InvalidOperationException("Annual rate must be greater than zero.");

            asset.DailyAccrualAnnualRatePercent = dto.DailyAccrualAnnualRatePercent.Value;
        }

        await _unitOfWork.Assets.UpdateAsync(asset);
        await _excelSyncService.RefreshAsync();

        return true;
    }

    public async Task<AssetDto?> UpdateAsync(int id, UpdateAssetDto dto)
    {
        var asset = await _unitOfWork.Assets.GetByIdAsync(id);
        if (asset == null) return null;

        var normalizedCode = NormalizeAssetCode(dto.AssetCode);
        var duplicate = await _unitOfWork.Assets.GetByCodeAsync(normalizedCode);
        if (duplicate != null && duplicate.AssetId != id)
            throw new InvalidOperationException($"Asset code '{normalizedCode}' already exists.");

        asset.AssetCode = normalizedCode;
        asset.AssetName = NormalizeAssetName(dto.AssetName);
        asset.AssetType = Enum.Parse<AssetType>(dto.AssetType, true);
        asset.Currency = dto.Currency;
        asset.ExternalTicker = dto.ExternalTicker;
        asset.Notes = dto.Notes;
        asset.IsDailyAccrualFund = dto.IsDailyAccrualFund;
        asset.DailyAccrualAnnualRatePercent = dto.IsDailyAccrualFund && dto.DailyAccrualAnnualRatePercent > 0
            ? dto.DailyAccrualAnnualRatePercent
            : 0m;
        asset.GoldCashbackPerGram = asset.AssetType == AssetType.Gold
            ? dto.GoldCashbackPerGram
            : 28.5m;
        asset.IsActive = dto.IsActive;

        await _unitOfWork.Assets.UpdateAsync(asset);
        await _excelSyncService.RefreshAsync();
        return _mapper.Map<AssetDto>(asset);
    }

    private static string NormalizeAssetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Asset code is required.");

        var normalized = ToAssetCode(code).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("Asset code must use English letters.");

        return normalized;
    }

    private static string NormalizeAssetName(string name)
    {
        var normalized = ToAscii(name).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("Asset name must use English letters.");

        return normalized;
    }

    private static string ToAscii(string value) => new(value.Where(c => c <= 127).ToArray());

    private static string ToAssetCode(string value) => new(value
        .Where(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
        .ToArray());

    public async Task<bool> DeleteAsync(int id)
    {
        var asset = await _unitOfWork.Assets.GetByIdAsync(id);
        if (asset == null) return false;
        await _unitOfWork.Assets.DeleteAsync(id);
        await _excelSyncService.RefreshAsync();
        return true;
    }

    public static AssetSummaryDto CalculateAssetSummary(Asset asset, List<Transaction> transactions, decimal currentPrice)
    {
        return asset.IsDailyAccrualFund
            ? CalculateDailyAccrualFundSummary(asset, transactions)
            : CalculateStandardAssetSummary(asset, transactions, currentPrice);
    }

    private static AssetSummaryDto CalculateStandardAssetSummary(Asset asset, List<Transaction> transactions, decimal currentPrice)
    {
        decimal unitsHeld = 0;
        decimal avgCost = 0;
        decimal realizedPnL = asset.ClosedRealizedPnL;
        decimal realizedCostBasis = 0;
        decimal totalFeesPaid = 0;
        var effectiveCurrentPrice = GetCurrentPrice(asset, currentPrice, DateTime.UtcNow);

        foreach (var txn in transactions.OrderBy(t => t.TransactionDate).ThenBy(t => t.TransactionId))
        {
            var feeAmount = txn.Fees;
            var goldPerGramAmount = asset.AssetType == AssetType.Gold
                ? txn.Quantity * txn.ManufacturingFeePerGram
                : 0m;

            if (txn.TransactionType == TransactionType.Buy)
            {
                var prevTotal = avgCost * unitsHeld;
                var newTotal = txn.TotalAmount + goldPerGramAmount + feeAmount;
                totalFeesPaid += feeAmount;
                unitsHeld += txn.Quantity;
                avgCost = unitsHeld > 0 ? (prevTotal + newTotal) / unitsHeld : 0;
            }
            else // Sell
            {
                totalFeesPaid += feeAmount;
                var saleProceeds = txn.TotalAmount + goldPerGramAmount - feeAmount;
                var soldCostBasis = avgCost * txn.Quantity;
                realizedCostBasis += soldCostBasis;
                realizedPnL += saleProceeds - soldCostBasis;
                unitsHeld -= txn.Quantity;
            }
        }

        var costBasis = avgCost * unitsHeld;
        var isClosedPosition = Math.Abs(unitsHeld) < ClosedPositionQuantityTolerance;
        var currentValue = asset.AssetType == AssetType.Gold
            ? unitsHeld * (effectiveCurrentPrice + asset.GoldCashbackPerGram)
            : unitsHeld * effectiveCurrentPrice;
        var unrealizedPnL = currentValue - costBasis;

        return new AssetSummaryDto
        {
            AssetId = asset.AssetId,
            AssetCode = asset.AssetCode,
            AssetName = asset.AssetName,
            AssetType = asset.AssetType.ToString(),
            IsDailyAccrualFund = false,
            DailyAccrualAnnualRatePercent = asset.DailyAccrualAnnualRatePercent,
            GoldCashbackPerGram = asset.GoldCashbackPerGram,
            IsClosedPosition = isClosedPosition,
            TotalUnitsHeld = Math.Round(unitsHeld, 4),
            AverageBuyPrice = Math.Round(unitsHeld > QuantityTolerance ? avgCost : 0m, 5),
            TotalCostBasis = Math.Round(costBasis, 2),
            TotalFeesPaid = Math.Round(totalFeesPaid, 2),
            TotalPaidIncludingFees = Math.Round(costBasis, 2),
            CurrentPrice = Math.Round(effectiveCurrentPrice, 5),
            CurrentValue = Math.Round(currentValue, 2),
            UnrealizedPnL = Math.Round(unrealizedPnL, 2),
            UnrealizedPnLPercent = costBasis != 0 ? Math.Round(unrealizedPnL / costBasis * 100, 2) : 0,
            RealizedPnL = Math.Round(realizedPnL, 2),
            RealizedPnLPercent = realizedCostBasis != 0 ? Math.Round(realizedPnL / realizedCostBasis * 100, 2) : 0,
            TotalPnL = Math.Round(unrealizedPnL + realizedPnL, 2),
            TotalPnLPercent = costBasis != 0 ? Math.Round((unrealizedPnL + realizedPnL) / costBasis * 100, 2) : 0
        };
    }

    private static AssetSummaryDto CalculateDailyAccrualFundSummary(Asset asset, List<Transaction> transactions)
    {
        decimal unitsHeld = 0;
        decimal avgCost = 0;
        decimal realizedPnL = asset.ClosedRealizedPnL;
        decimal realizedCostBasis = 0;
        decimal totalFeesPaid = 0;
        var accrualStartDate = GetDailyAccrualStartDate(asset, transactions);

        foreach (var txn in transactions.OrderBy(t => t.TransactionDate).ThenBy(t => t.TransactionId))
        {
            var unitPrice = GetDailyAccrualUnitPrice(asset, txn.TransactionDate, accrualStartDate);
            if (unitPrice <= 0)
            {
                continue;
            }

            var units = txn.TotalAmount / unitPrice;
            var feeAmount = txn.Fees;

            if (txn.TransactionType == TransactionType.Buy)
            {
                var prevTotal = avgCost * unitsHeld;
                var newTotal = txn.TotalAmount + feeAmount;
                totalFeesPaid += feeAmount;
                unitsHeld += units;
                avgCost = unitsHeld > 0 ? (prevTotal + newTotal) / unitsHeld : 0;
            }
            else
            {
                totalFeesPaid += feeAmount;
                var saleProceeds = txn.TotalAmount - feeAmount;
                var soldCostBasis = avgCost * units;
                realizedCostBasis += soldCostBasis;
                realizedPnL += saleProceeds - soldCostBasis;
                unitsHeld -= units;
            }
        }

        var effectiveCurrentPrice = GetDailyAccrualUnitPrice(asset, DateTime.UtcNow, accrualStartDate);
        var costBasis = avgCost * unitsHeld;
        var isClosedPosition = Math.Abs(unitsHeld) < ClosedPositionQuantityTolerance;
        var currentValue = unitsHeld * effectiveCurrentPrice;
        var unrealizedPnL = currentValue - costBasis;

        return new AssetSummaryDto
        {
            AssetId = asset.AssetId,
            AssetCode = asset.AssetCode,
            AssetName = asset.AssetName,
            AssetType = asset.AssetType.ToString(),
            IsDailyAccrualFund = true,
            DailyAccrualAnnualRatePercent = asset.DailyAccrualAnnualRatePercent,
            GoldCashbackPerGram = asset.GoldCashbackPerGram,
            IsClosedPosition = isClosedPosition,
            TotalUnitsHeld = Math.Round(unitsHeld, 4),
            AverageBuyPrice = Math.Round(unitsHeld > QuantityTolerance ? avgCost : 0m, 5),
            TotalCostBasis = Math.Round(costBasis, 2),
            TotalFeesPaid = Math.Round(totalFeesPaid, 2),
            TotalPaidIncludingFees = Math.Round(costBasis, 2),
            CurrentPrice = Math.Round(effectiveCurrentPrice, 4),
            CurrentValue = Math.Round(currentValue, 2),
            UnrealizedPnL = Math.Round(unrealizedPnL, 2),
            UnrealizedPnLPercent = costBasis != 0 ? Math.Round(unrealizedPnL / costBasis * 100, 2) : 0,
            RealizedPnL = Math.Round(realizedPnL, 2),
            RealizedPnLPercent = realizedCostBasis != 0 ? Math.Round(realizedPnL / realizedCostBasis * 100, 2) : 0,
            TotalPnL = Math.Round(unrealizedPnL + realizedPnL, 2),
            TotalPnLPercent = costBasis != 0 ? Math.Round((unrealizedPnL + realizedPnL) / costBasis * 100, 2) : 0
        };
    }

    public static decimal GetCurrentPrice(Asset asset, decimal fallbackPrice, DateTime asOf, DateTime? accrualStartDate = null)
    {
        if (!asset.IsDailyAccrualFund)
        {
            return fallbackPrice;
        }

        return GetDailyAccrualUnitPrice(asset, asOf, accrualStartDate ?? asset.CreatedAt.Date);
    }

    public static DateTime GetDailyAccrualStartDate(Asset asset, IEnumerable<Transaction> transactions, DateTime? candidateTransactionDate = null)
    {
        if (!asset.IsDailyAccrualFund)
        {
            return asset.CreatedAt.Date;
        }

        var dates = transactions.Select(t => t.TransactionDate.Date).ToList();
        if (candidateTransactionDate.HasValue)
        {
            dates.Add(candidateTransactionDate.Value.Date);
        }

        return dates.Count == 0 ? asset.CreatedAt.Date : dates.Min();
    }

    public static decimal GetDailyAccrualUnitPrice(Asset asset, DateTime asOf, DateTime? accrualStartDate = null)
    {
        var annualRate = asset.DailyAccrualAnnualRatePercent > 0 ? asset.DailyAccrualAnnualRatePercent : 16m;
        var anchorDate = (accrualStartDate ?? asset.CreatedAt).Date;
        var days = Math.Max(0d, (asOf.Date - anchorDate).TotalDays);
        var dailyGrowth = Math.Pow(1d + (double)annualRate / 100d, days / 365.25d);
        return Math.Round((decimal)dailyGrowth, 6);
    }
}
