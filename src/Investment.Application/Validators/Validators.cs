using FluentValidation;
using Investment.Application.DTOs;
using Investment.Domain.Enums;

namespace Investment.Application.Validators;

public class CreateAssetValidator : AbstractValidator<CreateAssetDto>
{
    public CreateAssetValidator()
    {
        RuleFor(x => x.AssetCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AssetName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AssetType).NotEmpty()
            .Must(x => Enum.TryParse<AssetType>(x, true, out _))
            .WithMessage("Invalid asset type. Valid types: Stock, Fund, Gold, RealEstate, Crypto, Other");
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
        RuleFor(x => x.ExternalTicker).MaximumLength(20);
    }
}

public class UpdateAssetValidator : AbstractValidator<UpdateAssetDto>
{
    public UpdateAssetValidator()
    {
        RuleFor(x => x.AssetCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AssetName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AssetType).NotEmpty()
            .Must(x => Enum.TryParse<AssetType>(x, true, out _))
            .WithMessage("Invalid asset type.");
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
    }
}

public class CreateTransactionValidator : AbstractValidator<CreateTransactionDto>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.AssetId).GreaterThan(0);
        RuleFor(x => x.TransactionType).NotEmpty()
            .Must(x => Enum.TryParse<TransactionType>(x, true, out _))
            .WithMessage("Invalid transaction type. Valid types: Buy, Sell");
        RuleFor(x => x.TransactionDate).NotEmpty().LessThanOrEqualTo(DateTime.UtcNow.AddDays(1));
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.PricePerUnit).GreaterThan(0);
        RuleFor(x => x.Fees).GreaterThanOrEqualTo(0);
    }
}

public class UpdateTransactionValidator : AbstractValidator<UpdateTransactionDto>
{
    public UpdateTransactionValidator()
    {
        RuleFor(x => x.AssetId).GreaterThan(0);
        RuleFor(x => x.TransactionType).NotEmpty()
            .Must(x => Enum.TryParse<TransactionType>(x, true, out _));
        RuleFor(x => x.TransactionDate).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.PricePerUnit).GreaterThan(0);
        RuleFor(x => x.Fees).GreaterThanOrEqualTo(0);
    }
}

public class CreatePriceValidator : AbstractValidator<CreatePriceDto>
{
    public CreatePriceValidator()
    {
        RuleFor(x => x.AssetId).GreaterThan(0);
        RuleFor(x => x.PriceDate).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public class CreateManualAssetValidator : AbstractValidator<CreateManualAssetDto>
{
  private static readonly string[] AllowedTypes = ["Gold", "Fund", "Other", "RealEstate", "Crypto"];

    public CreateManualAssetValidator()
    {
        RuleFor(x => x.AssetCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AssetName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AssetType).NotEmpty()
            .Must(x => AllowedTypes.Contains(x, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Manual assets support Gold, Fund, Other, RealEstate, or Crypto only.");
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
        RuleFor(x => x.InitialPrice).GreaterThan(0).When(x => x.InitialPrice.HasValue);
    }
}

public class SetAssetCurrentPriceValidator : AbstractValidator<SetAssetCurrentPriceDto>
{
    public SetAssetCurrentPriceValidator()
    {
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public class CreatePortfolioValidator : AbstractValidator<CreatePortfolioDto>
{
    public CreatePortfolioValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
