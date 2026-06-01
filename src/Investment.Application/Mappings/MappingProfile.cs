using AutoMapper;
using Investment.Application.DTOs;
using Investment.Domain.Entities;
using Investment.Domain.Enums;

namespace Investment.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Asset mappings
        CreateMap<Asset, AssetDto>()
            .ForMember(d => d.AssetType, opt => opt.MapFrom(s => s.AssetType.ToString()))
            .ForMember(d => d.Portfolios, opt => opt.MapFrom(s =>
                (s.PortfolioAssets ?? Array.Empty<PortfolioAsset>())
                    .Where(pa => pa.Portfolio != null)
                    .Select(pa => pa.Portfolio!.Name)
                    .ToList()));

        CreateMap<CreateAssetDto, Asset>()
            .ForMember(d => d.AssetType, opt => opt.MapFrom(s => Enum.Parse<AssetType>(s.AssetType, true)));

        CreateMap<UpdateAssetDto, Asset>()
            .ForMember(d => d.AssetType, opt => opt.MapFrom(s => Enum.Parse<AssetType>(s.AssetType, true)));

        // Transaction mappings
        CreateMap<Transaction, TransactionDto>()
            .ForMember(d => d.TransactionType, opt => opt.MapFrom(s => s.TransactionType.ToString()))
            .ForMember(d => d.AssetCode, opt => opt.MapFrom(s => s.Asset.AssetCode))
            .ForMember(d => d.AssetName, opt => opt.MapFrom(s => s.Asset.AssetName))
            .ForMember(d => d.AssetType, opt => opt.MapFrom(s => s.Asset.AssetType.ToString()))
            .ForMember(d => d.IsDailyAccrualFund, opt => opt.MapFrom(s => s.Asset.IsDailyAccrualFund));

        CreateMap<CreateTransactionDto, Transaction>()
            .ForMember(d => d.TransactionType, opt => opt.MapFrom(s => Enum.Parse<TransactionType>(s.TransactionType, true)));

        CreateMap<UpdateTransactionDto, Transaction>()
            .ForMember(d => d.TransactionType, opt => opt.MapFrom(s => Enum.Parse<TransactionType>(s.TransactionType, true)));

        // Price mappings
        CreateMap<Price, PriceDto>()
            .ForMember(d => d.Price, opt => opt.MapFrom(s => s.PriceValue))
            .ForMember(d => d.Source, opt => opt.MapFrom(s => s.Source.ToString()))
            .ForMember(d => d.AssetCode, opt => opt.MapFrom(s => s.Asset != null ? s.Asset.AssetCode : ""))
            .ForMember(d => d.AssetName, opt => opt.MapFrom(s => s.Asset != null ? s.Asset.AssetName : ""));

        CreateMap<CreatePriceDto, Price>()
            .ForMember(d => d.PriceValue, opt => opt.MapFrom(s => s.Price))
            .ForMember(d => d.Source, opt => opt.MapFrom(_ => PriceSource.Manual));

        // Portfolio mappings
        CreateMap<Portfolio, PortfolioDto>()
            .ForMember(d => d.Assets, opt => opt.MapFrom(s =>
                s.PortfolioAssets.Select(pa => pa.Asset).ToList()));

        CreateMap<CreatePortfolioDto, Portfolio>();
        CreateMap<UpdatePortfolioDto, Portfolio>();

        // PriceFetchLog mappings
        CreateMap<PriceFetchLog, PriceFetchLogDto>();
    }
}
