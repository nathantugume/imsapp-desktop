using imsapp_desktop.Services;

namespace imsapp_desktop.Models;

public class OutstandingOrder
{
    public int InvoiceNo { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public double NetTotal { get; set; }
    public decimal Paid { get; set; }
    public decimal Due { get; set; }
    public string OrderDate { get; set; } = string.Empty;

    public string DueFormatted => ServiceLocator.Branding.Current.FormatCurrency(Due);
    public string PaidFormatted => ServiceLocator.Branding.Current.FormatCurrency(Paid);
    public string NetTotalFormatted => ServiceLocator.Branding.Current.FormatCurrency(NetTotal);
}
