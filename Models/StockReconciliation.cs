using System;

namespace imsapp_desktop.Models;

public class StockReconciliation
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int SystemStock { get; set; }
    public int PhysicalCount { get; set; }
    public int Difference { get; set; }
    public DateTime? ReconciliationDate { get; set; }
    public string Status { get; set; } = "pending";
    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public int? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string StatusDisplay => Status switch { "approved" => "Approved", "rejected" => "Rejected", _ => "Pending" };
    public string DifferenceDisplay => Difference >= 0 ? $"+{Difference}" : Difference.ToString();
    public string DateDisplay => ReconciliationDate?.ToString("dd-MM-yyyy") ?? "";
}
