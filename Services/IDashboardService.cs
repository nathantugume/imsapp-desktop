namespace imsapp_desktop.Services;

public interface IDashboardService
{
    Task<DashboardStats> GetStatsAsync();
    Task<DailyProfitStats> GetTodayProfitAsync();
    Task<MonthlyProfitStats> GetMonthlyProfitAsync();
    Task<IReadOnlyList<ExpiryWarningItem>> GetExpiryWarningsAsync();
    Task<IReadOnlyList<CurrentStockItem>> GetCurrentStockAsync();
    Task<IReadOnlyList<RecentSaleItem>> GetRecentSalesAsync();
}

public class DashboardStats
{
    public int UserCount { get; set; }
    public int CategoryCount { get; set; }
    public int BrandCount { get; set; }
    public int ProductCount { get; set; }
    public int SupplierCount { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalOrderValue { get; set; }
    public decimal CashReceived { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal InventoryValue { get; set; }
    public decimal RetailValue { get; set; }
    public decimal WholesaleValue { get; set; }
}

public class DailyProfitStats
{
    public string Date { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public int SalesCount { get; set; }
}

public class MonthlyProfitStats
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public int SalesCount { get; set; }
}

public class ExpiryWarningItem
{
    public string ProductName { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public int Stock { get; set; }
    public int DaysLeft { get; set; }
    public bool IsExpired { get; set; }
    public string ExpiryDateDisplay => ExpiryDate.HasValue ? ExpiryDate.Value.ToString("MMM dd, yyyy") : "";
    public string DaysLeftDisplay => IsExpired ? " (Expired)" : $" - {DaysLeft} days left";
}

public class CurrentStockItem
{
    public string ProductName { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal Price { get; set; }
    public string Availability { get; set; } = string.Empty;
    public decimal BuyingPrice { get; set; }
}

public class RecentSaleItem
{
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Paid { get; set; }
    public decimal Balance { get; set; }
    public string OrderDate { get; set; } = string.Empty;
    public string PaidFormatted => ServiceLocator.Branding.Current?.FormatCurrency(Paid) ?? Paid.ToString("N2");
    public string BalanceFormatted => ServiceLocator.Branding.Current?.FormatCurrency(Balance) ?? Balance.ToString("N2");
}
