using System;

namespace imsapp_desktop.Models;

public class Brand
{
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string BStatus { get; set; } = "0";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string StatusDisplay => BStatus == "1" ? "Active" : "Inactive";
}
