using AutoMapper;
using Investment.Application.DTOs;
using Investment.Domain.Entities;
using Investment.Domain.Interfaces;

namespace Investment.Application.Services;

public interface IPortfolioService
{
    Task<IEnumerable<PortfolioDto>> GetAllAsync();
    Task<PortfolioDto?> GetByIdAsync(int id);
    Task<PortfolioSummaryDto?> GetSummaryAsync(int id);
    Task<PortfolioDto> CreateAsync(CreatePortfolioDto dto);
    Task<PortfolioDto?> UpdateAsync(int id, UpdatePortfolioDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> AddAssetAsync(int portfolioId, int assetId);
    Task<bool> RemoveAssetAsync(int portfolioId, int assetId);
}

public class PortfolioService : IPortfolioService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IExcelSyncService _excelSyncService;

    public PortfolioService(IUnitOfWork unitOfWork, IMapper mapper, IExcelSyncService excelSyncService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _excelSyncService = excelSyncService;
    }

    public async Task<IEnumerable<PortfolioDto>> GetAllAsync()
    {
        var portfolios = await _unitOfWork.Portfolios.GetAllAsync();
        return _mapper.Map<IEnumerable<PortfolioDto>>(portfolios);
    }

    public async Task<PortfolioDto?> GetByIdAsync(int id)
    {
        var portfolio = await _unitOfWork.Portfolios.GetByIdWithAssetsAsync(id);
        return portfolio == null ? null : _mapper.Map<PortfolioDto>(portfolio);
    }

    public async Task<PortfolioSummaryDto?> GetSummaryAsync(int id)
    {
        var portfolio = await _unitOfWork.Portfolios.GetByIdWithAssetsAsync(id);
        if (portfolio == null) return null;

        var assetIds = portfolio.PortfolioAssets.Select(pa => pa.AssetId).ToList();
        var transactionsByAsset = assetIds.Count == 0
            ? new Dictionary<int, List<Investment.Domain.Entities.Transaction>>()
            : (await _unitOfWork.Transactions.GetByAssetIdsOrderedAsync(assetIds))
                .GroupBy(t => t.AssetId)
                .ToDictionary(g => g.Key, g => g.ToList());
        var latestPrices = await _unitOfWork.Prices.GetLatestPricesForAssetsAsync(assetIds);
        var summaries = new List<AssetSummaryDto>();

        foreach (var pa in portfolio.PortfolioAssets)
        {
            var transactions = transactionsByAsset.TryGetValue(pa.AssetId, out var list)
                ? list
                : new List<Investment.Domain.Entities.Transaction>();
            var currentPrice = latestPrices.ContainsKey(pa.AssetId) ? latestPrices[pa.AssetId].PriceValue : 0;
            summaries.Add(AssetService.CalculateAssetSummary(pa.Asset, transactions, currentPrice));
        }

        return new PortfolioSummaryDto
        {
            PortfolioId = portfolio.PortfolioId,
            Name = portfolio.Name,
            TotalInvested = summaries.Sum(s => s.TotalCostBasis),
            CurrentValue = summaries.Sum(s => s.CurrentValue),
            UnrealizedPnL = summaries.Sum(s => s.UnrealizedPnL),
            UnrealizedPnLPercent = summaries.Sum(s => s.TotalCostBasis) != 0
                ? Math.Round(summaries.Sum(s => s.UnrealizedPnL) / summaries.Sum(s => s.TotalCostBasis) * 100, 2) : 0,
            RealizedPnL = summaries.Sum(s => s.RealizedPnL),
            AssetSummaries = summaries
        };
    }

    public async Task<PortfolioDto> CreateAsync(CreatePortfolioDto dto)
    {
        var portfolio = _mapper.Map<Portfolio>(dto);
        portfolio.CreatedAt = DateTime.UtcNow;
        var created = await _unitOfWork.Portfolios.AddAsync(portfolio);
        await _excelSyncService.RefreshAsync();
        return _mapper.Map<PortfolioDto>(created);
    }

    public async Task<PortfolioDto?> UpdateAsync(int id, UpdatePortfolioDto dto)
    {
        var portfolio = await _unitOfWork.Portfolios.GetByIdAsync(id);
        if (portfolio == null) return null;

        portfolio.Name = dto.Name;
        portfolio.Description = dto.Description;
        await _unitOfWork.Portfolios.UpdateAsync(portfolio);

        await _excelSyncService.RefreshAsync();

        var updated = await _unitOfWork.Portfolios.GetByIdWithAssetsAsync(id);
        return _mapper.Map<PortfolioDto>(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var portfolio = await _unitOfWork.Portfolios.GetByIdAsync(id);
        if (portfolio == null) return false;
        await _unitOfWork.Portfolios.DeleteAsync(id);
        await _excelSyncService.RefreshAsync();
        return true;
    }

    public async Task<bool> AddAssetAsync(int portfolioId, int assetId)
    {
        var portfolio = await _unitOfWork.Portfolios.GetByIdAsync(portfolioId);
        if (portfolio == null) return false;
        await _unitOfWork.Portfolios.AddAssetToPortfolioAsync(portfolioId, assetId);
        await _excelSyncService.RefreshAsync();
        return true;
    }

    public async Task<bool> RemoveAssetAsync(int portfolioId, int assetId)
    {
        await _unitOfWork.Portfolios.RemoveAssetFromPortfolioAsync(portfolioId, assetId);
        await _excelSyncService.RefreshAsync();
        return true;
    }
}
