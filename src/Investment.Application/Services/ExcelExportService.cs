using ClosedXML.Excel;
using Investment.Application.DTOs;

namespace Investment.Application.Services;

public interface IExcelExportService
{
    byte[] ExportHoldings(IEnumerable<HoldingDto> holdings);
    byte[] ExportTransactions(IEnumerable<TransactionDto> transactions);
    byte[] ExportFullWorkbook(PortfolioAnalyticsSummaryDto dashboard, IEnumerable<HoldingDto> holdings, IEnumerable<TransactionDto> transactions);
}

public class ExcelExportService : IExcelExportService
{
    public byte[] ExportHoldings(IEnumerable<HoldingDto> holdings)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Holdings");

        var headers = new[] { "Asset", "Type", "Units", "Avg Buy Price", "Current Price", "Total Cost", "Current Value", "Unrealized P&L", "Unrealized %", "Realized P&L", "Realized %" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.DarkBlue;
            worksheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var h in holdings)
        {
            worksheet.Cell(row, 1).Value = $"{h.AssetCode} - {h.AssetName}";
            worksheet.Cell(row, 2).Value = h.AssetType;
            worksheet.Cell(row, 3).Value = (double)h.TotalUnitsHeld;
            worksheet.Cell(row, 4).Value = (double)h.WeightedAverageBuyPrice;
            worksheet.Cell(row, 5).Value = (double)h.CurrentPrice;
            worksheet.Cell(row, 6).Value = (double)h.TotalCostBasis;
            worksheet.Cell(row, 7).Value = (double)h.CurrentValue;
            worksheet.Cell(row, 8).Value = (double)h.UnrealizedPnL;
            worksheet.Cell(row, 9).Value = (double)h.UnrealizedPnLPercent;
            worksheet.Cell(row, 10).Value = (double)h.RealizedPnL;
            worksheet.Cell(row, 11).Value = (double)h.RealizedPnLPercent;

            var pnlColor = h.UnrealizedPnL >= 0 ? XLColor.Green : XLColor.Red;
            worksheet.Cell(row, 8).Style.Font.FontColor = pnlColor;
            worksheet.Cell(row, 10).Style.Font.FontColor = h.RealizedPnL >= 0 ? XLColor.Green : XLColor.Red;

            row++;
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportTransactions(IEnumerable<TransactionDto> transactions)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Transactions");

        var headers = new[] { "Date", "Asset", "Type", "Quantity", "Price/Unit", "Total", "Fees", "Net Amount", "Notes" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.DarkBlue;
            worksheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var t in transactions)
        {
            worksheet.Cell(row, 1).Value = t.TransactionDate.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 2).Value = $"{t.AssetCode} - {t.AssetName}";
            worksheet.Cell(row, 3).Value = t.TransactionType;
            worksheet.Cell(row, 4).Value = (double)t.Quantity;
            worksheet.Cell(row, 5).Value = (double)t.PricePerUnit;
            worksheet.Cell(row, 6).Value = (double)t.TotalAmount;
            worksheet.Cell(row, 7).Value = (double)t.Fees;
            worksheet.Cell(row, 8).Value = (double)t.NetAmount;
            worksheet.Cell(row, 9).Value = t.Notes ?? "";

            var typeColor = t.TransactionType == "Buy" ? XLColor.Green : XLColor.Red;
            worksheet.Cell(row, 3).Style.Font.FontColor = typeColor;
            row++;
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportFullWorkbook(PortfolioAnalyticsSummaryDto dashboard, IEnumerable<HoldingDto> holdings, IEnumerable<TransactionDto> transactions)
    {
        var holdingsList = holdings.ToList();
        var transactionsList = transactions.ToList();

        using var workbook = new XLWorkbook();
        workbook.RightToLeft = true;

        // 1. Dashboard
        var dashboardSheet = workbook.Worksheets.Add("لوحة التحكم");
        WriteTitle(dashboardSheet, "ملخص إنڤيستي", 2);
        WriteHeaders(dashboardSheet, 3, "البند", "القيمة");
        WriteDashboardRow(dashboardSheet, 4, "إجمالي القيمة", dashboard.TotalCurrentValue, "money");
        WriteDashboardRow(dashboardSheet, 5, "إجمالي المدفوع", dashboard.TotalInvestedCapital, "money");
        WriteDashboardRow(dashboardSheet, 6, "الربح/الخسارة غير المحققة", dashboard.TotalUnrealizedPnL, "money");
        WriteDashboardRow(dashboardSheet, 7, "نسبة الربح/الخسارة غير المحققة", dashboard.TotalUnrealizedPnLPercent / 100m, "percent");
        WriteDashboardRow(dashboardSheet, 8, "الربح/الخسارة المحققة", dashboard.TotalRealizedPnL, "money");
        WriteDashboardRow(dashboardSheet, 9, "إجمالي الرسوم", dashboard.TotalFeesPaid, "money");
        WriteDashboardRow(dashboardSheet, 10, "العائد الكلي", dashboard.PortfolioReturnSinceInception / 100m, "percent");
        WriteDashboardRow(dashboardSheet, 11, "عدد الأصول", holdingsList.Count, "number");
        WriteDashboardRow(dashboardSheet, 12, "عدد العمليات", transactionsList.Count, "number");
        StyleDataRange(dashboardSheet, 3, 12, 2);

        // 2. Assets
        var assetsSheet = workbook.Worksheets.Add("الأصول");
        WriteTitle(assetsSheet, "حالة الأصول", 13);
        WriteHeaders(assetsSheet, 3, "الكود", "الاسم", "النوع", "السعر الحالي", "الوحدات", "إجمالي المدفوع", "القيمة السوقية", "الربح/الخسارة غير المحققة", "نسبة غير المحقق", "الربح/الخسارة المحققة", "إجمالي الربح/الخسارة", "نسبة إجمالية");
        for (var i = 0; i < holdingsList.Count; i++)
        {
            var row = i + 4;
            var item = holdingsList[i];
            assetsSheet.Cell(row, 1).Value = item.AssetCode;
            assetsSheet.Cell(row, 2).Value = item.AssetName;
            assetsSheet.Cell(row, 3).Value = AssetTypeLabel(item);
            assetsSheet.Cell(row, 4).Value = item.CurrentPrice;
            assetsSheet.Cell(row, 5).Value = item.TotalUnitsHeld;
            assetsSheet.Cell(row, 6).Value = item.TotalPaidIncludingFees;
            assetsSheet.Cell(row, 7).Value = item.CurrentValue;
            assetsSheet.Cell(row, 8).Value = item.UnrealizedPnL;
            assetsSheet.Cell(row, 9).Value = item.UnrealizedPnLPercent / 100m;
            assetsSheet.Cell(row, 10).Value = item.RealizedPnL;
            assetsSheet.Cell(row, 11).Value = item.TotalPnL;
            assetsSheet.Cell(row, 12).Value = item.TotalPnLPercent / 100m;
        }
        StyleDataRange(assetsSheet, 3, holdingsList.Count + 3, 12);
        FormatMoneyColumns(assetsSheet, 4, 6, 7, 8, 10, 11);
        FormatNumberColumns(assetsSheet, 5);
        FormatPercentColumns(assetsSheet, 9, 12);

        // 3. Transactions
        var transactionsSheet = workbook.Worksheets.Add("العمليات");
        WriteTitle(transactionsSheet, "سجل العمليات", 8);
        WriteHeaders(transactionsSheet, 3, "التاريخ", "الكود", "اسم الأصل", "النوع", "الوحدات", "سعر الوحدة", "الرسوم", "الصافي");
        for (var i = 0; i < transactionsList.Count; i++)
        {
            var row = i + 4;
            var t = transactionsList[i];
            transactionsSheet.Cell(row, 1).Value = t.TransactionDate;
            transactionsSheet.Cell(row, 2).Value = t.AssetCode;
            transactionsSheet.Cell(row, 3).Value = t.AssetName;
            transactionsSheet.Cell(row, 4).Value = TransactionTypeLabel(t);
            if (t.TransactionType != "Dividend")
            {
                transactionsSheet.Cell(row, 5).Value = t.Quantity;
                transactionsSheet.Cell(row, 6).Value = t.PricePerUnit;
                transactionsSheet.Cell(row, 7).Value = t.Fees;
            }
            transactionsSheet.Cell(row, 8).Value = t.NetAmount;
        }
        StyleDataRange(transactionsSheet, 3, transactionsList.Count + 3, 8);
        transactionsSheet.Column(1).Style.DateFormat.Format = "yyyy/mm/dd";
        FormatNumberColumns(transactionsSheet, 5);
        FormatMoneyColumns(transactionsSheet, 6, 7, 8);

        // 4. Analysis
        var analysisSheet = workbook.Worksheets.Add("تحليل الأنواع");
        WriteTitle(analysisSheet, "تحليل حسب نوع الأصل", 7);
        WriteHeaders(analysisSheet, 3, "النوع", "عدد الأصول", "إجمالي المدفوع", "القيمة السوقية", "غير المحقق", "المحقق", "الوزن");
        var groupedHoldings = holdingsList
            .GroupBy(h => AssetTypeLabel(h))
            .OrderByDescending(g => g.Sum(h => h.CurrentValue))
            .ToList();
        
        for (var i = 0; i < groupedHoldings.Count; i++)
        {
            var row = i + 4;
            var group = groupedHoldings[i];
            var currentValue = group.Sum(h => h.CurrentValue);
            analysisSheet.Cell(row, 1).Value = group.Key;
            analysisSheet.Cell(row, 2).Value = group.Count();
            analysisSheet.Cell(row, 3).Value = group.Sum(h => h.TotalPaidIncludingFees);
            analysisSheet.Cell(row, 4).Value = currentValue;
            analysisSheet.Cell(row, 5).Value = group.Sum(h => h.UnrealizedPnL);
            analysisSheet.Cell(row, 6).Value = group.Sum(h => h.RealizedPnL);
            analysisSheet.Cell(row, 7).Value = dashboard.TotalCurrentValue != 0 ? currentValue / dashboard.TotalCurrentValue : 0;
        }
        StyleDataRange(analysisSheet, 3, groupedHoldings.Count + 3, 7);
        FormatMoneyColumns(analysisSheet, 3, 4, 5, 6);
        FormatPercentColumns(analysisSheet, 7);

        foreach (var sheet in workbook.Worksheets)
        {
            sheet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            sheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            sheet.Columns().AdjustToContents();
            sheet.Rows().AdjustToContents();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteTitle(IXLWorksheet sheet, string title, int columns)
    {
        var range = sheet.Range(1, 1, 1, columns);
        range.Merge();
        range.Value = title;
        range.Style.Font.Bold = true;
        range.Style.Font.FontSize = 16;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#0f172a");
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Row(1).Height = 28;
    }

    private static void WriteHeaders(IXLWorksheet sheet, int row, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(row, i + 1).Value = headers[i];
        }
    }

    private static void WriteDashboardRow(IXLWorksheet sheet, int row, string label, decimal value, string format)
    {
        sheet.Cell(row, 1).Value = label;
        sheet.Cell(row, 2).Value = value;
        ApplyCellFormat(sheet.Cell(row, 2), format);
    }

    private static void WriteDashboardRow(IXLWorksheet sheet, int row, string label, int value, string format)
    {
        sheet.Cell(row, 1).Value = label;
        sheet.Cell(row, 2).Value = value;
        ApplyCellFormat(sheet.Cell(row, 2), format);
    }

    private static void StyleDataRange(IXLWorksheet sheet, int headerRow, int lastRow, int lastColumn)
    {
        var header = sheet.Range(headerRow, 1, headerRow, lastColumn);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#0f8f6f");
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        if (lastRow <= headerRow)
            return;

        var body = sheet.Range(headerRow + 1, 1, lastRow, lastColumn);
        body.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        body.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        body.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fbff");
    }

    private static void FormatMoneyColumns(IXLWorksheet sheet, params int[] columns)
    {
        foreach (var column in columns)
        {
            sheet.Column(column).Style.NumberFormat.Format = "#,##0.00";
        }
    }

    private static void FormatNumberColumns(IXLWorksheet sheet, params int[] columns)
    {
        foreach (var column in columns)
        {
            sheet.Column(column).Style.NumberFormat.Format = "#,##0.00";
        }
    }

    private static void FormatPercentColumns(IXLWorksheet sheet, params int[] columns)
    {
        foreach (var column in columns)
        {
            sheet.Column(column).Style.NumberFormat.Format = "0.00%";
        }
    }

    private static void ApplyCellFormat(IXLCell cell, string format)
    {
        cell.Style.NumberFormat.Format = format switch
        {
            "money" => "#,##0.00",
            "percent" => "0.00%",
            _ => "#,##0"
        };
    }

    private static string AssetTypeLabel(HoldingDto h) => h.IsDailyAccrualFund
        ? "Cloud"
        : h.AssetType switch
        {
            "Stock" => "Stock",
            "Gold" => "Gold",
            "Fund" => "Fund",
            _ => "Other"
        };

    private static string TransactionTypeLabel(TransactionDto t)
    {
        if (t.IsDailyAccrualFund)
            return t.TransactionType switch
            {
                "Buy" => "إيداع",
                "Sell" => "سحب",
                "Dividend" => "عائد قديم",
                _ => t.TransactionType
            };
        return t.TransactionType switch
        {
            "Dividend" => "أرباح",
            "Buy" => "شراء",
            "Sell" => "بيع",
            _ => t.TransactionType
        };
    }
}
