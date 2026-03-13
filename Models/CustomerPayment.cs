using System;
using imsapp_desktop.Services;

namespace imsapp_desktop.Models;

public class CustomerPayment
{
    public int Id { get; set; }
    public int InvoiceNo { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? CustomerName { get; set; }

    public string AmountFormatted => ServiceLocator.Branding.Current.FormatCurrency(AmountPaid);
    public string DateDisplay => PaymentDate?.ToString("dd-MM-yyyy HH:mm") ?? "";
}
