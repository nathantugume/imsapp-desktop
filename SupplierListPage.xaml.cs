using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using imsapp_desktop.Models;
using imsapp_desktop.ViewModels;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class SupplierListPage : Page
{
    public SupplierListPage()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadCommand.Execute(null);
    }

    private void SupplierGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SupplierGrid.SelectedItem is Supplier s)
            ViewModel.SelectedSupplier = s;
    }

    private void SupplierGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ViewModel.SelectedSupplier != null)
            EditSupplier_Click(sender, e);
    }

    private async void AddSupplier_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SupplierEditDialog(null);
        dialog.XamlRoot = XamlRoot;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.LoadAsync();
    }

    private async void EditSupplier_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedSupplier == null) return;
        var dialog = new SupplierEditDialog(ViewModel.SelectedSupplier);
        dialog.XamlRoot = XamlRoot;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.LoadAsync();
    }

    private async void DeleteSupplier_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedSupplier == null) return;
        var confirm = new ContentDialog
        {
            Title = "Delete Supplier",
            Content = $"Delete '{ViewModel.SelectedSupplier.SupplierName}'?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };
        confirm.XamlRoot = XamlRoot;
        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            await ServiceLocator.Suppliers.DeleteAsync(ViewModel.SelectedSupplier.SupplierId);
            await ViewModel.LoadAsync();
        }
    }

    private static readonly TableExportService.ExportColumn<Supplier>[] SupplierColumns =
    {
        new("ID", s => s.SupplierId.ToString()),
        new("Supplier", s => s.SupplierName),
        new("Contact", s => s.ContactPerson ?? ""),
        new("Phone", s => s.Phone ?? ""),
        new("Email", s => s.Email ?? ""),
        new("Status", s => s.StatusDisplay)
    };

    private async void ExportCsv_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Suppliers", "Suppliers", ViewModel.Suppliers.ToList(), SupplierColumns, "csv");
    private async void ExportExcel_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Suppliers", "Suppliers", ViewModel.Suppliers.ToList(), SupplierColumns, "excel");
    private async void ExportPdf_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Suppliers", "Suppliers", ViewModel.Suppliers.ToList(), SupplierColumns, "pdf");
}
