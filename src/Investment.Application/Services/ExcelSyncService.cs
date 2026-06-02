using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Investment.Application.DTOs;
using Investment.Domain.Entities;
using Investment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Investment.Application.Services;

public interface IExcelSyncService
{
    Task RefreshAsync();
}

public class ExcelSyncService : IExcelSyncService
{
    private const string WorkbookFileName = "Investy.xlsx";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExcelSyncService> _logger;

    public ExcelSyncService(IUnitOfWork unitOfWork, ILogger<ExcelSyncService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task RefreshAsync()
    {
        try
        {
            _logger.LogInformation("Starting Excel workbook refresh...");

            var assets = (await _unitOfWork.Assets.GetAllAsync()).ToList();
            var transactions = (await _unitOfWork.Transactions.GetAllAsync()).ToList();

            var assetIds = assets.Select(a => a.AssetId).ToList();
            var latestPrices = assetIds.Count == 0
                ? new Dictionary<int, Price>()
                : await _unitOfWork.Prices.GetLatestPricesForAssetsAsync(assetIds);

            var holdings = BuildHoldings(assets, transactions, latestPrices);
            var summary = BuildSummary(holdings, transactions);

            using var workbook = new XLWorkbook();
            workbook.Properties.Title = "Investy Portfolio Snapshot";
            workbook.Properties.Subject = "Investment portfolio transactions, holdings, and dashboard";
            workbook.Properties.Author = "Investy";
            workbook.Properties.Company = "Investy";

            WriteTransactionsSheet(workbook, transactions);
            WriteAssetsStateSheet(workbook, holdings);
            WriteDashboardSheet(workbook, summary, holdings);

            var workbookPath = ResolveWorkbookPath();
            var directory = Path.GetDirectoryName(workbookPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            workbook.SaveAs(workbookPath);
            MakeWorkbookRightToLeft(workbookPath);

            _logger.LogInformation("Excel workbook saved to {Path}", workbookPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Excel workbook snapshot.");
        }
    }

    private static List<AssetSummaryDto> BuildHoldings(
        IReadOnlyCollection<Asset> assets,
        IReadOnlyCollection<Transaction> transactions,
        IReadOnlyDictionary<int, Price> latestPrices)
    {
        var transactionsByAsset = transactions
            .GroupBy(t => t.AssetId)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.TransactionDate).ThenBy(t => t.TransactionId).ToList());

        var holdings = new List<AssetSummaryDto>();

        foreach (var asset in assets)
        {
            if (!transactionsByAsset.TryGetValue(asset.AssetId, out var assetTransactions) || assetTransactions.Count == 0)
            {
                continue;
            }

            var currentPrice = latestPrices.TryGetValue(asset.AssetId, out var price) ? price.PriceValue : 0m;
            holdings.Add(AssetService.CalculateAssetSummary(asset, assetTransactions, currentPrice));
        }

        return holdings;
    }

    private static PortfolioAnalyticsSummaryDto BuildSummary(
        IReadOnlyCollection<AssetSummaryDto> holdings,
        IReadOnlyCollection<Transaction> transactions)
    {
        var totalInvested = holdings.Sum(h => h.TotalCostBasis);
        var totalCurrentValue = holdings.Sum(h => h.CurrentValue);
        var totalUnrealizedPnL = holdings.Sum(h => h.UnrealizedPnL);
        var totalRealizedPnL = holdings.Sum(h => h.RealizedPnL);
        var totalFees = transactions.Sum(t => t.Fees);
        var totalReturnSinceInception = totalUnrealizedPnL + totalRealizedPnL;

        return new PortfolioAnalyticsSummaryDto
        {
            TotalInvestedCapital = Math.Round(totalInvested, 2),
            TotalCurrentValue = Math.Round(totalCurrentValue, 2),
            TotalUnrealizedPnL = Math.Round(totalUnrealizedPnL, 2),
            TotalUnrealizedPnLPercent = totalInvested != 0 ? Math.Round(totalUnrealizedPnL / totalInvested * 100, 2) : 0,
            TotalRealizedPnL = Math.Round(totalRealizedPnL, 2),
            TotalFeesPaid = Math.Round(totalFees, 2),
            PortfolioReturnSinceInception = totalInvested != 0 ? Math.Round(totalReturnSinceInception / totalInvested * 100, 2) : 0,
        };
    }

    private static void WriteTransactionsSheet(XLWorkbook workbook, IReadOnlyCollection<Transaction> transactions)
    {
        var worksheet = workbook.Worksheets.Add("Transactions");
        WriteHeaders(worksheet, "التاريخ", "كود الأصل", "اسم الأصل", "النوع", "الكمية", "السعر", "صافي المبلغ", "الرسوم", "ملاحظات");

        var row = 2;
        foreach (var transaction in transactions.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.TransactionId))
        {
            worksheet.Cell(row, 1).Value = transaction.TransactionDate;
            worksheet.Cell(row, 2).Value = transaction.Asset == null ? string.Empty : transaction.Asset.AssetCode;
            worksheet.Cell(row, 3).Value = transaction.Asset == null ? string.Empty : transaction.Asset.AssetName;
            worksheet.Cell(row, 4).Value = transaction.TransactionType.ToString() == "Buy" ? "شراء" : transaction.TransactionType.ToString() == "Sell" ? "بيع" : transaction.TransactionType.ToString();
            worksheet.Cell(row, 5).Value = transaction.Quantity;
            worksheet.Cell(row, 6).Value = transaction.PricePerUnit;
            worksheet.Cell(row, 7).Value = transaction.NetAmount;
            worksheet.Cell(row, 8).Value = transaction.Fees;
            worksheet.Cell(row, 9).Value = transaction.Notes ?? string.Empty;
            row++;
        }

        ApplyDataSheetStyle(worksheet, row - 1, 9, "TransactionsTable", XLColor.FromHtml("#0F766E"));
        worksheet.Column(1).Style.DateFormat.Format = "yyyy-mm-dd";
        ApplyNumberFormat(worksheet, 2, row - 1, 5, 8);
    }

    private static void WriteAssetsStateSheet(XLWorkbook workbook, IReadOnlyCollection<AssetSummaryDto> summaries)
    {
        var worksheet = workbook.Worksheets.Add("Assets State");
        WriteHeaders(worksheet, "كود الأصل", "نوع الأصل", "متوسط الشراء", "السعر الحالي", "الكمية", "إجمالي المدفوع شامل الرسوم", "القيمة السوقية", "إجمالي الربح/الخسارة غير المحققة", "نسبة الربح/الخسارة غير المحققة", "إجمالي الربح/الخسارة المحققة", "نسبة الربح/الخسارة المحققة");

        var row = 2;
        foreach (var summary in summaries.OrderByDescending(x => x.CurrentValue))
        {
            worksheet.Cell(row, 1).Value = summary.AssetCode;
            worksheet.Cell(row, 2).Value = summary.AssetType;
            worksheet.Cell(row, 3).Value = summary.AverageBuyPrice;
            worksheet.Cell(row, 4).Value = summary.CurrentPrice;
            worksheet.Cell(row, 5).Value = summary.TotalUnitsHeld;
            worksheet.Cell(row, 6).Value = summary.TotalPaidIncludingFees;
            worksheet.Cell(row, 7).Value = summary.CurrentValue;
            worksheet.Cell(row, 8).Value = summary.UnrealizedPnL;
            worksheet.Cell(row, 9).Value = summary.UnrealizedPnLPercent;
            worksheet.Cell(row, 10).Value = summary.RealizedPnL;
            worksheet.Cell(row, 11).Value = summary.RealizedPnLPercent;
            row++;
        }

        ApplyDataSheetStyle(worksheet, row - 1, 11, "AssetsStateTable", XLColor.FromHtml("#1D4ED8"));
        ApplyNumberFormat(worksheet, 2, row - 1, 3, 11);
        worksheet.Column(9).Style.NumberFormat.Format = "0.00\"%\"";
        worksheet.Column(11).Style.NumberFormat.Format = "0.00\"%\"";
    }

    private static void WriteDashboardSheet(XLWorkbook workbook, PortfolioAnalyticsSummaryDto summary, IReadOnlyCollection<AssetSummaryDto> holdings)
    {
        var worksheet = workbook.Worksheets.Add("Dashboard");

        worksheet.Cell(1, 1).Value = "Investy Dashboard";
        worksheet.Range(1, 1, 1, 6).Merge();
        worksheet.Cell(1, 1).Style
            .Font.SetBold()
            .Font.SetFontSize(18)
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        worksheet.Cell(2, 1).Value = $"Last refresh: {DateTime.Now:yyyy-MM-dd HH:mm}";
        worksheet.Range(2, 1, 2, 6).Merge();
        worksheet.Cell(2, 1).Style
            .Font.SetFontColor(XLColor.FromHtml("#475569"))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        var cards = new (string Label, decimal Value, string Format, XLColor Color)[]
        {
            ("إجمالي المدفوع", summary.TotalInvestedCapital, "#,##0.00", XLColor.FromHtml("#2563EB")),
            ("إجمالي القيمة", summary.TotalCurrentValue, "#,##0.00", XLColor.FromHtml("#16A34A")),
            ("الربح/الخسارة غير المحققة", summary.TotalUnrealizedPnL, "#,##0.00", summary.TotalUnrealizedPnL >= 0 ? XLColor.FromHtml("#16A34A") : XLColor.FromHtml("#DC2626")),
            ("نسبة الربح/الخسارة", summary.TotalUnrealizedPnLPercent, "0.00\"%\"", summary.TotalUnrealizedPnLPercent >= 0 ? XLColor.FromHtml("#16A34A") : XLColor.FromHtml("#DC2626")),
            ("الربح/الخسارة المحققة", summary.TotalRealizedPnL, "#,##0.00", summary.TotalRealizedPnL >= 0 ? XLColor.FromHtml("#16A34A") : XLColor.FromHtml("#DC2626")),
            ("إجمالي الرسوم", summary.TotalFeesPaid, "#,##0.00", XLColor.FromHtml("#D97706"))
        };

        for (var i = 0; i < cards.Length; i++)
        {
            var startColumn = (i % 3) * 2 + 1;
            var startRow = i < 3 ? 4 : 7;
            worksheet.Range(startRow, startColumn, startRow, startColumn + 1).Merge();
            worksheet.Range(startRow + 1, startColumn, startRow + 1, startColumn + 1).Merge();
            worksheet.Cell(startRow, startColumn).Value = cards[i].Label;
            worksheet.Cell(startRow + 1, startColumn).Value = cards[i].Value;
            worksheet.Cell(startRow + 1, startColumn).Style.NumberFormat.Format = cards[i].Format;

            var cardRange = worksheet.Range(startRow, startColumn, startRow + 1, startColumn + 1);
            cardRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cardRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            cardRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            cardRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cardRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Cell(startRow, startColumn).Style.Font.FontColor = XLColor.FromHtml("#475569");
            worksheet.Cell(startRow + 1, startColumn).Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(cards[i].Color);
        }

        var row = 11;
        WriteHeaders(worksheet, row, "كود الأصل", "نوع الأصل", "الكمية", "السعر الحالي", "القيمة السوقية", "نسبة الوزن");
        row++;

        var totalCurrentValue = holdings.Sum(h => h.CurrentValue);
        foreach (var holding in holdings.OrderByDescending(h => h.CurrentValue))
        {
            worksheet.Cell(row, 1).Value = holding.AssetCode;
            worksheet.Cell(row, 2).Value = holding.AssetType;
            worksheet.Cell(row, 3).Value = holding.TotalUnitsHeld;
            worksheet.Cell(row, 4).Value = holding.CurrentPrice;
            worksheet.Cell(row, 5).Value = holding.CurrentValue;
            worksheet.Cell(row, 6).Value = totalCurrentValue == 0 ? 0 : holding.CurrentValue / totalCurrentValue * 100;
            row++;
        }

        ApplyDataSheetStyle(worksheet, row - 1, 6, "DashboardAssetsTable", XLColor.FromHtml("#7C3AED"), headerRow: 11);
        ApplyNumberFormat(worksheet, 12, row - 1, 3, 6);
        worksheet.Column(6).Style.NumberFormat.Format = "0.00\"%\"";
        worksheet.SheetView.FreezeRows(11);
    }

    private static void WriteHeaders(IXLWorksheet worksheet, params string[] headers)
    {
        WriteHeaders(worksheet, 1, headers);
    }

    private static void WriteHeaders(IXLWorksheet worksheet, int rowIndex, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(rowIndex, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
    }

    private static void ApplyDataSheetStyle(IXLWorksheet worksheet, int lastRow, int lastColumn, string tableName, XLColor headerColor, int headerRow = 1)
    {
        worksheet.Style.Font.FontName = "Cairo";
        worksheet.Style.Alignment.ReadingOrder = XLAlignmentReadingOrderValues.RightToLeft;
        worksheet.SheetView.FreezeRows(headerRow);

        if (lastRow >= headerRow)
        {
            var table = worksheet.Range(headerRow, 1, lastRow, lastColumn).CreateTable(tableName);
            table.Theme = XLTableTheme.TableStyleMedium2;
            table.ShowAutoFilter = true;
        }

        var headerRange = worksheet.Range(headerRow, 1, headerRow, lastColumn);
        headerRange.Style.Fill.BackgroundColor = headerColor;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        var usedRange = worksheet.Range(headerRow, 1, Math.Max(headerRow, lastRow), lastColumn);
        usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#94A3B8");

        worksheet.Columns(1, lastColumn).AdjustToContents();
        worksheet.Rows(headerRow, Math.Max(headerRow, lastRow)).AdjustToContents();
    }

    private static void ApplyNumberFormat(IXLWorksheet worksheet, int firstRow, int lastRow, int firstColumn, int lastColumn)
    {
        if (lastRow < firstRow)
        {
            return;
        }

        worksheet.Range(firstRow, firstColumn, lastRow, lastColumn).Style.NumberFormat.Format = "#,##0.00";
    }

    private static void MakeWorkbookRightToLeft(string workbookPath)
    {
        using var document = SpreadsheetDocument.Open(workbookPath, true);
        foreach (var worksheetPart in document.WorkbookPart?.WorksheetParts ?? Enumerable.Empty<WorksheetPart>())
        {
            var sheetViews = worksheetPart.Worksheet.GetFirstChild<SheetViews>() ?? worksheetPart.Worksheet.PrependChild(new SheetViews());
            var sheetView = sheetViews.GetFirstChild<SheetView>() ?? sheetViews.AppendChild(new SheetView { WorkbookViewId = 0U });
            sheetView.RightToLeft = true;
            worksheetPart.Worksheet.Save();
        }
    }

    private static string ResolveWorkbookPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, WorkbookFileName);
            if (File.Exists(candidate) || File.Exists(Path.Combine(current.FullName, "Investment.slnx")))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), WorkbookFileName);
    }
}
