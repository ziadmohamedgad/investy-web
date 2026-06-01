using Investment.Domain.Entities;

namespace Investment.Domain.Interfaces;

public interface IPriceFetchOrchestrator
{
    Task<PriceFetchLog> ExecuteFetchAsync(bool isIntraday = false);
}
