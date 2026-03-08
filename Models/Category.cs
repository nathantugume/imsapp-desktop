using System;

namespace imsapp_desktop.Models;

public class Category
{
    public int CatId { get; set; }
    public int MainCat { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = "0";
    public DateTime CreatedAt { get; set; }

    public string StatusDisplay => Status == "1" ? "Active" : "Inactive";
}
