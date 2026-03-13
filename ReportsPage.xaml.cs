using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Services;
using imsapp_desktop.ViewModels;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace imsapp_desktop;

public sealed partial class ReportsPage : Page
{
    public ReportsPage()
    {
        InitializeComponent();
        var now = System.DateTime.Now;
        DatePicker.Date = new System.DateTimeOffset(now);
        MonthPicker.Date = new System.DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, System.TimeSpan.Zero);
        YearBox.Value = now.Year;
    }

    private ReportsViewModel GetViewModel() => (ReportsViewModel)DataContext;

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        var vm = GetViewModel();
        vm.SelectedFilter = FilterCombo.SelectedItem as string ?? "daily";
        DatePicker.Visibility = Visibility.Visible;
        MonthPicker.Visibility = Visibility.Collapsed;
        YearBox.Visibility = Visibility.Collapsed;

        vm.SelectedDate = DatePicker.Date.DateTime.ToString("yyyy-MM-dd");
        vm.SelectedMonth = MonthPicker.Date.DateTime.ToString("yyyy-MM");
        vm.SelectedYear = (int)YearBox.Value;
        vm.LoadCommand.Execute(null);
    }

    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilterCombo.SelectedItem is not string filter) return;
        if (DataContext is not ReportsViewModel vm || DatePicker == null || MonthPicker == null || YearBox == null)
            return; // Not yet initialized

        vm.SelectedFilter = filter;

        DatePicker.Visibility = filter == "daily" ? Visibility.Visible : Visibility.Collapsed;
        MonthPicker.Visibility = filter == "monthly" ? Visibility.Visible : Visibility.Collapsed;
        YearBox.Visibility = filter == "yearly" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyFilter_Click(object sender, RoutedEventArgs e)
    {
        var vm = GetViewModel();
        vm.SelectedDate = DatePicker.Date.DateTime.ToString("yyyy-MM-dd");
        vm.SelectedMonth = MonthPicker.Date.DateTime.ToString("yyyy-MM");
        vm.SelectedYear = (int)YearBox.Value;
        vm.LoadCommand.Execute(null);
    }

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var (file, vm) = await PickSaveFileAsync("CSV file", ".csv");
        if (file == null || vm == null) return;

        var csv = ReportExportService.ExportToCsv(vm.Rows.ToList(), vm.ReportTitle, vm.ReportPeriod);
        await FileIO.WriteTextAsync(file, csv);

        await ShowExportSuccessAsync(vm.Rows.Count, file.Name);
    }

    private async void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        var (file, vm) = await PickSaveFileAsync("Excel workbook", ".xlsx");
        if (file == null || vm == null) return;

        var path = file.Path;
        if (string.IsNullOrEmpty(path))
        {
            await ShowErrorAsync("Could not get file path for save location.");
            return;
        }

        ReportExportService.ExportToExcel(vm.Rows.ToList(), vm.ReportTitle, vm.ReportPeriod, path);
        await ShowExportSuccessAsync(vm.Rows.Count, file.Name);
    }

    private async void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var (file, vm) = await PickSaveFileAsync("PDF document", ".pdf");
        if (file == null || vm == null) return;

        var path = file.Path;
        if (string.IsNullOrEmpty(path))
        {
            await ShowErrorAsync("Could not get file path for save location.");
            return;
        }

        await Task.Run(() => ReportExportService.ExportToPdf(vm.Rows.ToList(), vm.ReportTitle, vm.ReportPeriod, path));
        await ShowExportSuccessAsync(vm.Rows.Count, file.Name);
    }

    private async Task<(StorageFile? file, ReportsViewModel? vm)> PickSaveFileAsync(string choiceLabel, string extension)
    {
        var vm = GetViewModel();
        if (vm.Rows.Count == 0)
        {
            await ShowErrorAsync("No data to export. Load a report first.");
            return (null, null);
        }

        var window = App.MainWindowInstance;
        if (window == null)
        {
            await ShowErrorAsync("Could not open save dialog.");
            return (null, null);
        }

        var baseName = $"Report_{vm.ReportTitle.Replace(" ", "_")}_{DateTime.Now:yyyy-MM-dd}";
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = baseName,
            FileTypeChoices = { { choiceLabel, new[] { extension } } }
        };
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

        var file = await picker.PickSaveFileAsync();
        return (file, file != null ? vm : null);
    }

    private async Task ShowExportSuccessAsync(int rowCount, string fileName)
    {
        var dlg = new ContentDialog { Title = "Export", Content = $"Exported {rowCount} rows to {fileName}", CloseButtonText = "OK" };
        dlg.XamlRoot = XamlRoot;
        await dlg.ShowAsync();
    }

    private async Task ShowErrorAsync(string message)
    {
        var dlg = new ContentDialog { Title = "Export", Content = message, CloseButtonText = "OK" };
        dlg.XamlRoot = XamlRoot;
        await dlg.ShowAsync();
    }
}
