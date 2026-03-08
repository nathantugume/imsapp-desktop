using imsapp_desktop.Services;

namespace imsapp_desktop.Models;

public class ReportRow
{
    public int InvoiceNo { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public double Subtotal { get; set; }
    public double Gst { get; set; }
    public double Discount { get; set; }
    public double NetTotal { get; set; }
    public decimal Paid { get; set; }
    public decimal Due { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public int ItemCount { get; set; }

    public string NetTotalFormatted => ServiceLocator.Branding.Current.FormatCurrency(NetTotal);
    public string PaidFormatted => ServiceLocator.Branding.Current.FormatCurrency(Paid);
    public string DueFormatted => ServiceLocator.Branding.Current.FormatCurrency(Due);
}
