using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboard = ServiceLocator.Dashboard;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;

    [ObservableProperty] private int _userCount;
    [ObservableProperty] private int _categoryCount;
    [ObservableProperty] private int _brandCount;
    [ObservableProperty] private int _productCount;
    [ObservableProperty] private int _supplierCount;
    [ObservableProperty] private int _orderCount;
    [ObservableProperty] private decimal _totalOrderValue;
    [ObservableProperty] private decimal _cashReceived;
    [ObservableProperty] private decimal _outstandingBalance;
    [ObservableProperty] private decimal _inventoryValue;
    [ObservableProperty] private decimal _retailValue;
    [ObservableProperty] private decimal _wholesaleValue;

    [ObservableProperty] private decimal _todayRevenue;
    [ObservableProperty] private decimal _todayCost;
    [ObservableProperty] private decimal _todayProfit;
    [ObservableProperty] private int _todaySalesCount;

    [ObservableProperty] private decimal _monthlyRevenue;
    [ObservableProperty] private decimal _monthlyCost;
    [ObservableProperty] private decimal _monthlyProfit;
    [ObservableProperty] private decimal _profitMarginPercent;

    [ObservableProperty] private decimal _potentialRetailProfit;
    [ObservableProperty] private decimal _potentialWholesaleProfit;
    [ObservableProperty] private decimal _potentialProfitMarginPercent;

    public ObservableCollection<ExpiryWarningItem> ExpiryWarnings { get; } = new();
    public ObservableCollection<CurrentStockItem> CurrentStock { get; } = new();
    public ObservableCollection<RecentSaleItem> RecentSales { get; } = new();

    public string CategoriesBrandsSummary => $"{CategoryCount} / {BrandCount}";
    public bool HasNoExpiryWarnings => ExpiryWarnings.Count == 0;
    public string CurrentUserName => ServiceLocator.Auth.CurrentUser?.Name ?? "User";
    public string TotalOrderValueFormatted => ServiceLocator.Branding.Current.FormatCurrency(TotalOrderValue);
    public string CashReceivedFormatted => ServiceLocator.Branding.Current.FormatCurrency(CashReceived);
    public string OutstandingFormatted => ServiceLocator.Branding.Current.FormatCurrency(OutstandingBalance);
    public string TodayProfitFormatted => ServiceLocator.Branding.Current.FormatCurrency(TodayProfit);
    public string TodayRevenueFormatted => ServiceLocator.Branding.Current.FormatCurrency(TodayRevenue);
    public string TodayCostFormatted => ServiceLocator.Branding.Current.FormatCurrency(TodayCost);
    public string MonthlyProfitFormatted => ServiceLocator.Branding.Current.FormatCurrency(MonthlyProfit);
    public string MonthlyRevenueFormatted => ServiceLocator.Branding.Current.FormatCurrency(MonthlyRevenue);
    public string MonthlyCostFormatted => ServiceLocator.Branding.Current.FormatCurrency(MonthlyCost);
    public string ProfitMarginFormatted => ProfitMarginPercent.ToString("N2") + "%";
    public string InventoryValueFormatted => ServiceLocator.Branding.Current.FormatCurrency(InventoryValue);
    public string PotentialProfitFormatted => ServiceLocator.Branding.Current.FormatCurrency(PotentialRetailProfit);
    public string WelcomeMessage => $"Welcome, {CurrentUserName} | {TodayDate}";
    public bool IsMaster => string.Equals(ServiceLocator.Auth.CurrentUser?.Role, "Master", StringComparison.OrdinalIgnoreCase);
    public string TodayDate => DateTime.Now.ToString("MMM dd, yyyy");
    public string TodayDayOfWeek => DateTime.Now.DayOfWeek.ToString();

    partial void OnCategoryCountChanged(int value) => OnPropertyChanged(nameof(CategoriesBrandsSummary));
    partial void OnBrandCountChanged(int value) => OnPropertyChanged(nameof(CategoriesBrandsSummary));

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var stats = await _dashboard.GetStatsAsync();
            UserCount = stats.UserCount;
            CategoryCount = stats.CategoryCount;
            BrandCount = stats.BrandCount;
            ProductCount = stats.ProductCount;
            SupplierCount = stats.SupplierCount;
            OrderCount = stats.OrderCount;
            TotalOrderValue = stats.TotalOrderValue;
            CashReceived = stats.CashReceived;
            OutstandingBalance = stats.OutstandingBalance;
            InventoryValue = stats.InventoryValue;
            RetailValue = stats.RetailValue;
            WholesaleValue = stats.WholesaleValue;

            var today = await _dashboard.GetTodayProfitAsync();
            TodayRevenue = today.Revenue;
            TodayCost = today.Cost;
            TodayProfit = today.Profit;
            TodaySalesCount = today.SalesCount;

            var monthly = await _dashboard.GetMonthlyProfitAsync();
            MonthlyRevenue = monthly.Revenue;
            MonthlyCost = monthly.Cost;
            MonthlyProfit = monthly.Profit;
            ProfitMarginPercent = monthly.Revenue > 0 ? Math.Round((monthly.Profit / monthly.Revenue) * 100, 2) : 0;

            PotentialRetailProfit = RetailValue - InventoryValue;
            PotentialWholesaleProfit = WholesaleValue - InventoryValue;
            PotentialProfitMarginPercent = RetailValue > 0 ? Math.Round((PotentialRetailProfit / RetailValue) * 100, 2) : 0;

            ExpiryWarnings.Clear();
            foreach (var w in await _dashboard.GetExpiryWarningsAsync())
                ExpiryWarnings.Add(w);
            OnPropertyChanged(nameof(HasNoExpiryWarnings));

            CurrentStock.Clear();
            var allStock = await _dashboard.GetCurrentStockAsync();
            foreach (var s in allStock)
                CurrentStock.Add(s);

            RecentSales.Clear();
            foreach (var s in await _dashboard.GetRecentSalesAsync())
                RecentSales.Add(s);

            OnPropertyChanged(nameof(CurrentUserName));
            OnPropertyChanged(nameof(IsMaster));
            OnPropertyChanged(nameof(TodayDate));
            OnPropertyChanged(nameof(TodayDayOfWeek));
            OnPropertyChanged(nameof(TotalOrderValueFormatted));
            OnPropertyChanged(nameof(CashReceivedFormatted));
            OnPropertyChanged(nameof(OutstandingFormatted));
            OnPropertyChanged(nameof(TodayProfitFormatted));
            OnPropertyChanged(nameof(TodayRevenueFormatted));
            OnPropertyChanged(nameof(TodayCostFormatted));
            OnPropertyChanged(nameof(MonthlyProfitFormatted));
            OnPropertyChanged(nameof(MonthlyRevenueFormatted));
            OnPropertyChanged(nameof(MonthlyCostFormatted));
            OnPropertyChanged(nameof(ProfitMarginFormatted));
            OnPropertyChanged(nameof(InventoryValueFormatted));
            OnPropertyChanged(nameof(PotentialProfitFormatted));
            OnPropertyChanged(nameof(WelcomeMessage));
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to load dashboard: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
