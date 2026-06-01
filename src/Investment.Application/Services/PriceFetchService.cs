using AutoMapper;
using Investment.Application.DTOs;
using Investment.Domain.Interfaces;

namespace Investment.Application.Services;

public interface IPriceFetchService
{
    Task<PriceFetchLogDto> RunFetchAsync();
    Task<IEnumerable<PriceFetchLogDto>> GetLogsAsync();
    Task<PriceFetchStatusDto> GetStatusAsync();
}

public class PriceFetchService : IPriceFetchService
{
    private readonly IPriceFetchOrchestrator _orchestrator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PriceFetchService(IPriceFetchOrchestrator orchestrator, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _orchestrator = orchestrator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PriceFetchLogDto> RunFetchAsync()
    {
        var log = await _orchestrator.ExecuteFetchAsync();
        return _mapper.Map<PriceFetchLogDto>(log);
    }

    public async Task<IEnumerable<PriceFetchLogDto>> GetLogsAsync()
    {
        var logs = await _unitOfWork.PriceFetchLogs.GetLatestAsync(10);
        return _mapper.Map<IEnumerable<PriceFetchLogDto>>(logs);
    }

    public async Task<PriceFetchStatusDto> GetStatusAsync()
    {
        var activeCount = await _unitOfWork.Assets.CountActiveAssetsAsync();
        var assetsWithTicker = await _unitOfWork.Assets.CountActiveStockAssetsWithTickerAsync();
        var lastLog = await _unitOfWork.PriceFetchLogs.GetLastSuccessfulAsync();
        var today = DateTime.UtcNow.Date;
        var latestLogs = await _unitOfWork.PriceFetchLogs.GetLatestAsync(100);
        var dailyApiCallsUsed = latestLogs
            .Where(l => l.Success && l.FetchDate.Date == today)
            .Sum(l => l.TotalAssets);

        return new PriceFetchStatusDto
        {
            CurrentMode = "EODHD",
            LastRunTime = lastLog?.FetchDate,
            ActiveAssetCount = activeCount,
            AssetsWithTicker = assetsWithTicker,
            DailyApiCallsUsed = dailyApiCallsUsed,
            LastAssetsUpdated = lastLog?.AssetsUpdated
        };
    }
}
