using imsapp_desktop.Services;

namespace imsapp_desktop.Models;

public class ReportSummary
{
    public double TotalSales { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalDue { get; set; }
    public int OrderCount { get; set; }
    public double Revenue { get; set; }
    public double Cost { get; set; }
    public double Profit { get; set; }

    public string TotalSalesFormatted => ServiceLocator.Branding.Current.FormatCurrency(TotalSales);
    public string TotalPaidFormatted => ServiceLocator.Branding.Current.FormatCurrency(TotalPaid);
    public string TotalDueFormatted => ServiceLocator.Branding.Current.FormatCurrency(TotalDue);
    public string RevenueFormatted => ServiceLocator.Branding.Current.FormatCurrency(Revenue);
    public string CostFormatted => ServiceLocator.Branding.Current.FormatCurrency(Cost);
    public string ProfitFormatted => ServiceLocator.Branding.Current.FormatCurrency(Profit);
}
