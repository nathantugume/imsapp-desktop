using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Models;
using imsapp_desktop.Services;
using imsapp_desktop.ViewModels;

namespace imsapp_desktop;

public sealed partial class StockReconciliationPage : Page
{
    public StockReconciliationPage()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadCommand.Execute(null);
    }

    private void ReconciliationGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReconciliationGrid.SelectedItem is StockReconciliation r)
            ViewModel.SelectedReconciliation = r;
    }

    private async void NewReconciliation_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CreateReconciliationDialog();
        dialog.XamlRoot = XamlRoot;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.LoadAsync();
    }

    private async void Approve_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ApproveCommand.ExecuteAsync(null);
    }

    private async void Reject_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RejectCommand.ExecuteAsync(null);
    }

    private static readonly TableExportService.ExportColumn<StockReconciliation>[] ReconciliationColumns =
    {
        new("ID", r => r.Id.ToString()),
        new("Product", r => r.ProductName),
        new("System", r => r.SystemStock.ToString()),
        new("Physical", r => r.PhysicalCount.ToString()),
        new("Diff", r => r.DifferenceDisplay),
        new("Status", r => r.StatusDisplay),
        new("Date", r => r.DateDisplay),
        new("Created By", r => r.CreatedByName ?? "")
    };

    private async void ExportCsv_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Stock Reconciliation", "StockReconciliation", ViewModel.Reconciliations.ToList(), ReconciliationColumns, "csv");
    private async void ExportExcel_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Stock Reconciliation", "StockReconciliation", ViewModel.Reconciliations.ToList(), ReconciliationColumns, "excel");
    private async void ExportPdf_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Stock Reconciliation", "StockReconciliation", ViewModel.Reconciliations.ToList(), ReconciliationColumns, "pdf");
}
