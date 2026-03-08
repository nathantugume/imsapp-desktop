using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using imsapp_desktop.Data;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public class ReportService : IReportService
{
    public async Task<(IReadOnlyList<ReportRow> Rows, ReportSummary Summary, string Title, string Period)> GetDailyReportAsync(string date)
    {
        // date: yyyy-MM-dd -> convert to DD-MM-YYYY for order_date
        if (!DateTime.TryParse(date, out var dt))
            dt = DateTime.Today;
        var datePattern = dt.ToString("dd-MM-yyyy") + "%";

        return await GetReportAsync(datePattern, "Daily Report", dt.ToString("MMMM dd, yyyy"), dt);
    }

    public async Task<(IReadOnlyList<ReportRow> Rows, ReportSummary Summary, string Title, string Period)> GetMonthlyReportAsync(string yearMonth)
    {
        // yearMonth: yyyy-MM
        var parts = yearMonth.Split('-');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var month))
        {
            var now = DateTime.Now;
            year = now.Year;
            month = now.Month;
        }
        var monthPattern = "%-" + month.ToString("D2") + "-" + year;
        var period = new DateTime(year, month, 1).ToString("MMMM yyyy");

        return await GetReportAsync(monthPattern, "Monthly Report", period, null, year, month);
    }

    public async Task<(IReadOnlyList<ReportRow> Rows, ReportSummary Summary, string Title, string Period)> GetYearlyReportAsync(int year)
    {
        var yearPattern = "%-%-" + year;
        return await GetReportAsync(yearPattern, "Yearly Report", year.ToString(), null, year, null);
    }

    private async Task<(IReadOnlyList<ReportRow> Rows, ReportSummary Summary, string Title, string Period)> GetReportAsync(
        string orderDatePattern, string title, string period,
        DateTime? forDaily = null, int? forYear = null, int? forMonth = null)
    {
        const string ordersSql = @"
            SELECT o.invoice_no AS InvoiceNo, o.customer_name AS CustomerName, o.subtotal AS Subtotal,
                o.gst AS Gst, o.discount AS Discount, o.net_total AS NetTotal, o.paid AS Paid, o.due AS Due,
                o.payment_method AS PaymentMethod, o.order_date AS OrderDate,
                COUNT(i.id) AS ItemCount
            FROM orders o
            LEFT JOIN invoices i ON o.invoice_no = i.invoice_no
            WHERE o.order_date LIKE @Pattern
            GROUP BY o.invoice_no
            ORDER BY o.order_date DESC";

        using var conn = DatabaseFactory.CreateConnection();
        var rows = (await conn.QueryAsync<ReportRow>(ordersSql, new { Pattern = orderDatePattern })).ToList();

        var totalSales = rows.Sum(r => r.NetTotal);
        var totalPaid = rows.Sum(r => r.Paid);
        var totalDue = rows.Sum(r => r.Due);

        double revenue = 0, cost = 0;
        string profitSql;
        object profitParams;

        // Use buying_price only (unit_cost may not exist in older schema)
        const string profitSqlTemplate = @"
            SELECT SUM(i.order_qty * i.price_per_item) AS revenue,
                SUM(i.order_qty * COALESCE(p.buying_price, 0)) AS cost
            FROM invoices i
            INNER JOIN products p ON i.product_name = p.product_name
            INNER JOIN orders o ON i.invoice_no = o.invoice_no
            WHERE o.order_date LIKE @Pattern
            AND COALESCE(p.buying_price, 0) > 0";
        if (forDaily.HasValue)
        {
            profitSql = profitSqlTemplate;
            profitParams = new { Pattern = orderDatePattern };
        }
        else if (forYear.HasValue && forMonth.HasValue)
        {
            profitSql = profitSqlTemplate;
            profitParams = new { Pattern = "%-" + forMonth.Value.ToString("D2") + "-" + forYear };
        }
        else if (forYear.HasValue)
        {
            profitSql = profitSqlTemplate;
            profitParams = new { Pattern = "%-%-" + forYear };
        }
        else
        {
            profitParams = new { Pattern = orderDatePattern };
            profitSql = profitSqlTemplate;
        }

        var profitRow = await conn.QueryFirstOrDefaultAsync<ProfitRow>(profitSql, profitParams);
        if (profitRow != null)
        {
            revenue = (double)(profitRow.Revenue ?? 0);
            cost = (double)(profitRow.Cost ?? 0);
        }

        var summary = new ReportSummary
        {
            TotalSales = totalSales,
            TotalPaid = totalPaid,
            TotalDue = totalDue,
            OrderCount = rows.Count,
            Revenue = revenue,
            Cost = cost,
            Profit = revenue - cost
        };

        return (rows, summary, title, period);
    }

    private class ProfitRow { public decimal? Revenue { get; set; } public decimal? Cost { get; set; } }
}
