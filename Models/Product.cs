using System;
using imsapp_desktop.Services;

namespace imsapp_desktop.Models;

public class Product
{
    public int Pid { get; set; }
    public int? CatId { get; set; }
    public int? BrandId { get; set; }
    public int? SupplierId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Stock { get; set; }
    public double Price { get; set; }
    public decimal? WholesalePrice { get; set; }
    public string Unit { get; set; } = "pieces";
    public string? PurchaseUnit { get; set; }
    public string? SaleUnit { get; set; }
    public int ConversionFactor { get; set; } = 1;
    public decimal? UnitCost { get; set; }
    public double BuyingPrice { get; set; }
    public string Description { get; set; } = string.Empty;
    public string PStatus { get; set; } = "1";
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? CategoryName { get; set; }
    public string? BrandName { get; set; }
    public string? SupplierName { get; set; }

    public string PriceFormatted => ServiceLocator.Branding.Current.FormatCurrency(Price);
    public string BuyingPriceFormatted => ServiceLocator.Branding.Current.FormatCurrency(BuyingPrice);
    public string WholesalePriceFormatted => WholesalePrice.HasValue && WholesalePrice > 0
        ? ServiceLocator.Branding.Current.FormatCurrency((double)WholesalePrice.Value)
        : "—";
    public string SaleUnitDisplay => !string.IsNullOrEmpty(SaleUnit) ? SaleUnit : (Unit ?? "pieces");
    public string StatusDisplay => PStatus == "1" ? "Active" : "Inactive";
}
