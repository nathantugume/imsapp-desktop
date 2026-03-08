using System;

namespace imsapp_desktop.Models;

public class Supplier
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = "1";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string StatusDisplay => Status == "1" ? "Active" : "Inactive";
}
