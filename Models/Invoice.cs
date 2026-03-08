using System;

namespace imsapp_desktop.Models;

public class Invoice
{
    public int Id { get; set; }
    public int InvoiceNo { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int OrderQty { get; set; }
    public double PricePerItem { get; set; }
    public DateTime CreatedAt { get; set; }
}
