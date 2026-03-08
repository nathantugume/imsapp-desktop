using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using imsapp_desktop.Models;
using imsapp_desktop.ViewModels;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class BrandListPage : Page
{
    public BrandListPage()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadCommand.Execute(null);
    }

    private void BrandGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BrandGrid.SelectedItem is Brand b)
            ViewModel.SelectedBrand = b;
    }

    private void BrandGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ViewModel.SelectedBrand != null)
            EditBrand_Click(sender, e);
    }

    private async void AddBrand_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new BrandEditDialog(null);
        dialog.XamlRoot = XamlRoot;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.LoadAsync();
    }

    private async void EditBrand_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedBrand == null) return;
        var dialog = new BrandEditDialog(ViewModel.SelectedBrand);
        dialog.XamlRoot = XamlRoot;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.LoadAsync();
    }

    private async void DeleteBrand_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedBrand == null) return;
        var confirm = new ContentDialog
        {
            Title = "Delete Brand",
            Content = $"Delete '{ViewModel.SelectedBrand.BrandName}'?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };
        confirm.XamlRoot = XamlRoot;
        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            await ServiceLocator.Brands.DeleteAsync(ViewModel.SelectedBrand.BrandId);
            await ViewModel.LoadAsync();
        }
    }

    private static readonly TableExportService.ExportColumn<Brand>[] BrandColumns =
    {
        new("ID", b => b.BrandId.ToString()),
        new("Brand Name", b => b.BrandName),
        new("Status", b => b.StatusDisplay)
    };

    private async void ExportCsv_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Brands", "Brands", ViewModel.Brands.ToList(), BrandColumns, "csv");
    private async void ExportExcel_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Brands", "Brands", ViewModel.Brands.ToList(), BrandColumns, "excel");
    private async void ExportPdf_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Brands", "Brands", ViewModel.Brands.ToList(), BrandColumns, "pdf");
}
