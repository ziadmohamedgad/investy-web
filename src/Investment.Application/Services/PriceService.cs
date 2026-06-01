using AutoMapper;
using Investment.Application.DTOs;
using Investment.Domain.Entities;
using Investment.Domain.Enums;
using Investment.Domain.Interfaces;

namespace Investment.Application.Services;

public interface IPriceService
{
    Task<IEnumerable<PriceDto>> GetAllAsync();
    Task<IEnumerable<PriceDto>> GetByAssetIdAsync(int assetId);
    Task<PriceDto> CreateAsync(CreatePriceDto dto);
    Task<int> BulkCreateAsync(BulkPriceDto dto);
}

public class PriceService : IPriceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IExcelSyncService _excelSyncService;

    public PriceService(IUnitOfWork unitOfWork, IMapper mapper, IExcelSyncService excelSyncService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _excelSyncService = excelSyncService;
    }

    public async Task<IEnumerable<PriceDto>> GetAllAsync()
    {
        var prices = await _unitOfWork.Prices.GetAllAsync();
        return _mapper.Map<IEnumerable<PriceDto>>(prices);
    }

    public async Task<IEnumerable<PriceDto>> GetByAssetIdAsync(int assetId)
    {
        var prices = await _unitOfWork.Prices.GetByAssetIdAsync(assetId);
        return _mapper.Map<IEnumerable<PriceDto>>(prices);
    }

    public async Task<PriceDto> CreateAsync(CreatePriceDto dto)
    {
        var price = _mapper.Map<Price>(dto);
        price.Source = PriceSource.Manual;
        price.CreatedAt = DateTime.UtcNow;

        var created = await _unitOfWork.Prices.AddAsync(price);
        await _excelSyncService.RefreshAsync();
        var result = await _unitOfWork.Prices.GetByIdAsync(created.PriceId);
        return _mapper.Map<PriceDto>(result);
    }

    public async Task<int> BulkCreateAsync(BulkPriceDto dto)
    {
        var prices = new List<Price>();
        foreach (var item in dto.Prices)
        {
            var asset = await _unitOfWork.Assets.GetByCodeAsync(item.AssetCode);
            if (asset == null) continue;

            prices.Add(new Price
            {
                AssetId = asset.AssetId,
                PriceDate = item.Date,
                PriceValue = item.Price,
                Source = PriceSource.Manual,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (prices.Count > 0)
        {
            await _unitOfWork.Prices.AddRangeAsync(prices);
            await _excelSyncService.RefreshAsync();
        }

        return prices.Count;
    }
}
