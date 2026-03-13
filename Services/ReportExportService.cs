using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

/// <summary>
/// Exports report data to CSV, Excel, or PDF (delegates to TableExportService).
/// </summary>
public static class ReportExportService
{
    private static readonly TableExportService.ExportColumn<ReportRow>[] ReportColumns =
    {
        new("Invoice", r => r.InvoiceNo.ToString()),
        new("Customer", r => r.CustomerName),
        new("Subtotal", r => r.Subtotal.ToString("N2")),
        new("GST", r => r.Gst.ToString("N2")),
        new("Discount", r => r.Discount.ToString("N2")),
        new("Net Total", r => r.NetTotal.ToString("N2")),
        new("Paid", r => r.Paid.ToString("N2")),
        new("Due", r => r.Due.ToString("N2")),
        new("Payment Method", r => r.PaymentMethod),
        new("Order Date", r => r.OrderDate),
        new("Item Count", r => r.ItemCount.ToString())
    };

    public static string ExportToCsv(IReadOnlyList<ReportRow> rows, string reportTitle, string reportPeriod) =>
        TableExportService.ExportToCsv(rows, ReportColumns);

    public static void ExportToExcel(IReadOnlyList<ReportRow> rows, string reportTitle, string reportPeriod, string filePath) =>
        TableExportService.ExportToExcel(rows, ReportColumns, reportTitle, reportPeriod, filePath);

    public static void ExportToPdf(IReadOnlyList<ReportRow> rows, string reportTitle, string reportPeriod, string filePath) =>
        TableExportService.ExportToPdf(rows, ReportColumns, reportTitle, reportPeriod, filePath);
}
