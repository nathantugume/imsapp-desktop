using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly IReportService _service = ServiceLocator.Reports;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _reportTitle = "Daily Report";
    [ObservableProperty] private string _reportPeriod = "";
    [ObservableProperty] private string _selectedFilter = "daily";
    [ObservableProperty] private string _selectedDate = "";
    [ObservableProperty] private string _selectedMonth = "";
    [ObservableProperty] private int _selectedYear;

    [ObservableProperty] private int _orderCount;
    [ObservableProperty] private string _totalSalesFormatted = "";
    [ObservableProperty] private string _revenueFormatted = "";
    [ObservableProperty] private string _profitFormatted = "";

    public ObservableCollection<ReportRow> Rows { get; } = new();

    public ReportsViewModel()
    {
        var now = DateTime.Now;
        _selectedDate = now.ToString("yyyy-MM-dd");
        _selectedMonth = now.ToString("yyyy-MM");
        _selectedYear = now.Year;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var (rows, summary, title, period) = SelectedFilter switch
            {
                "monthly" => await _service.GetMonthlyReportAsync(SelectedMonth),
                "yearly" => await _service.GetYearlyReportAsync(SelectedYear),
                _ => await _service.GetDailyReportAsync(SelectedDate)
            };

            ReportTitle = title;
            ReportPeriod = period;

            OrderCount = summary.OrderCount;
            TotalSalesFormatted = summary.TotalSalesFormatted;
            RevenueFormatted = summary.RevenueFormatted;
            ProfitFormatted = summary.ProfitFormatted;

            Rows.Clear();
            foreach (var r in rows)
                Rows.Add(r);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to load report: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
