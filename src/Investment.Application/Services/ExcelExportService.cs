using ClosedXML.Excel;
using Investment.Application.DTOs;

namespace Investment.Application.Services;

public interface IExcelExportService
{
    byte[] ExportHoldings(IEnumerable<HoldingDto> holdings);
    byte[] ExportTransactions(IEnumerable<TransactionDto> transactions);
}

public class ExcelExportService : IExcelExportService
{
    public byte[] ExportHoldings(IEnumerable<HoldingDto> holdings)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Holdings");

        // Headers
        var headers = new[] { "Asset", "Type", "Units", "Avg Buy Price", "Current Price", "Total Cost", "Current Value", "Unrealized P&L", "Unrealized %", "Realized P&L", "Total P&L %" };
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
            worksheet.Cell(row, 11).Value = (double)h.TotalPnLPercent;

            // Color P&L cells
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
}
