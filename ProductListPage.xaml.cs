using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Windows.Storage;
using CommunityToolkit.WinUI.UI.Controls;
using imsapp_desktop.ViewModels;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class ProductListPage : Page
{
    private const string ColumnVisibilityKey = "ProductListColumnVisibility";
    private static readonly string[] AllColumnKeys = { "ID", "Product", "Category", "Brand", "Supplier", "Stock", "Unit", "Buying", "Retail", "Wholesale", "Status" };

    private readonly Dictionary<string, CheckBox> _columnToggles = new();

    public ProductListPage()
    {
        InitializeComponent();
        _columnToggles["ID"] = ColId;
        _columnToggles["Product"] = ColProduct;
        _columnToggles["Category"] = ColCategory;
        _columnToggles["Brand"] = ColBrand;
        _columnToggles["Supplier"] = ColSupplier;
        _columnToggles["Stock"] = ColStock;
        _columnToggles["Unit"] = ColUnit;
        _columnToggles["Buying"] = ColBuying;
        _columnToggles["Retail"] = ColRetail;
        _columnToggles["Wholesale"] = ColWholesale;
        _columnToggles["Status"] = ColStatus;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        LoadColumnVisibility();
        BuildColumns();
        ViewModel.LoadCommand.Execute(null);
    }

    private void LoadColumnVisibility()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;
            var saved = settings.Values[ColumnVisibilityKey] as string;
            if (string.IsNullOrEmpty(saved)) return;
            var hidden = new HashSet<string>(saved.Split(',', StringSplitOptions.RemoveEmptyEntries));
            foreach (var key in AllColumnKeys)
            {
                if (_columnToggles.TryGetValue(key, out var toggle))
                    toggle.IsChecked = !hidden.Contains(key);
            }
        }
        catch { /* ignore */ }
    }

    private static bool IsChecked(CheckBox? cb) => cb?.IsChecked == true;

    private void SaveColumnVisibility()
    {
        try
        {
            var hidden = AllColumnKeys.Where(k => !(_columnToggles.TryGetValue(k, out var t) && IsChecked(t))).ToList();
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values[ColumnVisibilityKey] = string.Join(",", hidden);
        }
        catch { /* ignore */ }
    }

    private void BuildColumns()
    {
        ProductGrid.Columns.Clear();
        foreach (var key in AllColumnKeys)
        {
            if (!_columnToggles.TryGetValue(key, out var toggle) || !IsChecked(toggle)) continue;
            var col = CreateColumn(key);
            if (col != null) ProductGrid.Columns.Add(col);
        }
    }

    private static DataGridTextColumn? CreateColumn(string key)
    {
        return key switch
        {
            "ID" => new DataGridTextColumn { Header = "ID", Binding = new Binding { Path = new PropertyPath("Pid") }, Width = new DataGridLength(50) },
            "Product" => new DataGridTextColumn { Header = "Product", Binding = new Binding { Path = new PropertyPath("ProductName") }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) },
            "Category" => new DataGridTextColumn { Header = "Category", Binding = new Binding { Path = new PropertyPath("CategoryName") }, Width = new DataGridLength(100) },
            "Brand" => new DataGridTextColumn { Header = "Brand", Binding = new Binding { Path = new PropertyPath("BrandName") }, Width = new DataGridLength(90) },
            "Supplier" => new DataGridTextColumn { Header = "Supplier", Binding = new Binding { Path = new PropertyPath("SupplierName") }, Width = new DataGridLength(90) },
            "Stock" => new DataGridTextColumn { Header = "Stock", Binding = new Binding { Path = new PropertyPath("Stock") }, Width = new DataGridLength(70) },
            "Unit" => new DataGridTextColumn { Header = "Unit", Binding = new Binding { Path = new PropertyPath("SaleUnitDisplay") }, Width = new DataGridLength(70) },
            "Buying" => new DataGridTextColumn { Header = "Buying", Binding = new Binding { Path = new PropertyPath("BuyingPriceFormatted") }, Width = new DataGridLength(90) },
            "Retail" => new DataGridTextColumn { Header = "Retail", Binding = new Binding { Path = new PropertyPath("PriceFormatted") }, Width = new DataGridLength(90) },
            "Wholesale" => new DataGridTextColumn { Header = "Wholesale", Binding = new Binding { Path = new PropertyPath("WholesalePriceFormatted") }, Width = new DataGridLength(90) },
            "Status" => new DataGridTextColumn { Header = "Status", Binding = new Binding { Path = new PropertyPath("StatusDisplay") }, Width = new DataGridLength(70) },
            _ => null
        };
    }

    private void ColumnToggle_Changed(object sender, RoutedEventArgs e)
    {
        var visibleCount = AllColumnKeys.Count(k => _columnToggles.TryGetValue(k, out var t) && IsChecked(t));
        if (visibleCount == 0 && sender is CheckBox cb)
        {
            cb.IsChecked = true;
            return;
        }
        SaveColumnVisibility();
        BuildColumns();
    }

    private void ProductGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductGrid.SelectedItem is Product p)
            ViewModel.SelectedProduct = p;
    }

    private void ProductGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ViewModel.SelectedProduct != null)
            EditProduct_Click(sender, e);
    }

    private async void AddProduct_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ProductEditDialog(null);
        dialog.XamlRoot = XamlRoot;
        await dialog.ShowAsync();
        await ViewModel.LoadAsync();
    }

    private async void AddStock_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProduct == null) return;
        var dialog = new AddStockDialog(ViewModel.SelectedProduct);
        dialog.XamlRoot = XamlRoot;
        await dialog.ShowAsync();
        await ViewModel.LoadAsync();
    }

    private async void ViewProduct_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProduct == null) return;
        var dialog = new ViewProductDialog(ViewModel.SelectedProduct);
        dialog.XamlRoot = XamlRoot;
        await dialog.ShowAsync();
    }

    private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProduct == null) return;
        var confirm = new ContentDialog
        {
            Title = "Delete Product",
            Content = $"Delete '{ViewModel.SelectedProduct.ProductName}'?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };
        confirm.XamlRoot = XamlRoot;
        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            await ServiceLocator.Products.DeleteAsync(ViewModel.SelectedProduct.Pid);
            await ViewModel.LoadAsync();
        }
    }

    private async void EditProduct_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProduct == null) return;
        var dialog = new ProductEditDialog(ViewModel.SelectedProduct);
        dialog.XamlRoot = XamlRoot;
        await dialog.ShowAsync();
        await ViewModel.LoadAsync();
    }

    private static readonly TableExportService.ExportColumn<Product>[] ProductColumns =
    {
        new("ID", p => p.Pid.ToString()),
        new("Product", p => p.ProductName),
        new("Category", p => p.CategoryName ?? ""),
        new("Brand", p => p.BrandName ?? ""),
        new("Supplier", p => p.SupplierName ?? ""),
        new("Stock", p => p.Stock.ToString()),
        new("Unit", p => p.SaleUnitDisplay),
        new("Buying", p => p.BuyingPriceFormatted),
        new("Retail", p => p.PriceFormatted),
        new("Wholesale", p => p.WholesalePriceFormatted),
        new("Status", p => p.StatusDisplay)
    };

    private async void ExportCsv_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Products", "Products", ViewModel.Products.ToList(), ProductColumns, "csv");
    private async void ExportExcel_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Products", "Products", ViewModel.Products.ToList(), ProductColumns, "excel");
    private async void ExportPdf_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Products", "Products", ViewModel.Products.ToList(), ProductColumns, "pdf");
}
