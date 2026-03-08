using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Models;
using imsapp_desktop.ViewModels;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class ProductEditDialog : ContentDialog
{
    private readonly Product? _product;

    public ProductEditDialog(Product? product)
    {
        InitializeComponent();
        _product = product;
        Title = product == null ? "Add Product" : "Edit Product";

        Loaded += async (_, _) =>
        {
            await LoadDropdownsAsync();
            if (product != null)
                PopulateForm(product);
        };
    }

    private void PopulateForm(Product product)
    {
        ProductNameBox.Text = product.ProductName;
        StockBox.Value = product.Stock;
        PriceBox.Value = product.Price;
        WholesalePriceBox.Value = (double)(product.WholesalePrice ?? 0);
        BuyingPriceBox.Value = product.BuyingPrice;
        DescriptionBox.Text = product.Description;
        StatusCombo.SelectedIndex = product.PStatus == "1" ? 0 : 1;
        if (!string.IsNullOrEmpty(product.SaleUnit))
        {
            var idx = SaleUnitCombo.Items.IndexOf(product.SaleUnit);
            if (idx >= 0) SaleUnitCombo.SelectedIndex = idx;
        }
        if (!string.IsNullOrEmpty(product.PurchaseUnit))
        {
            var puIdx = PurchaseUnitCombo.Items.IndexOf(product.PurchaseUnit);
            if (puIdx >= 0) PurchaseUnitCombo.SelectedIndex = puIdx;
            else PurchaseUnitCombo.SelectedIndex = 0;
        }
        if (product.ConversionFactor > 0) ConversionFactorBox.Value = product.ConversionFactor;
        if (product.ExpiryDate.HasValue) ExpiryDatePicker.Date = new DateTimeOffset(product.ExpiryDate.Value);
        SelectComboItem(CategoryCombo, product.CatId);
        SelectComboItem(BrandCombo, product.BrandId);
        SelectComboItem(SupplierCombo, product.SupplierId);
    }

    private async Task LoadDropdownsAsync()
    {
        var categories = await ServiceLocator.Categories.GetAllAsync();
        var brands = await ServiceLocator.Brands.GetAllAsync();
        var suppliers = await ServiceLocator.Suppliers.GetAllAsync();

        CategoryCombo.Items.Clear();
        CategoryCombo.Items.Add(new Category { CatId = 0, CategoryName = "Uncategorized" });
        foreach (var c in categories) CategoryCombo.Items.Add(c);

        BrandCombo.Items.Clear();
        BrandCombo.Items.Add(new Brand { BrandId = 0, BrandName = "No Brand" });
        foreach (var b in brands) BrandCombo.Items.Add(b);

        SupplierCombo.Items.Clear();
        SupplierCombo.Items.Add(new Supplier { SupplierId = 0, SupplierName = "No Supplier" });
        foreach (var s in suppliers) SupplierCombo.Items.Add(s);

        if (CategoryCombo.Items.Count > 0) CategoryCombo.SelectedIndex = 0;
        if (BrandCombo.Items.Count > 0) BrandCombo.SelectedIndex = 0;
        if (SupplierCombo.Items.Count > 0) SupplierCombo.SelectedIndex = 0;
    }

    private void SelectComboItem(ComboBox combo, int? id)
    {
        if (id == null || id == 0) { combo.SelectedIndex = 0; return; }
        for (int i = 0; i < combo.Items.Count; i++)
        {
            var item = combo.Items[i];
            int? itemId = item switch
            {
                Category c => c.CatId,
                Brand b => b.BrandId,
                Supplier s => s.SupplierId,
                _ => null
            };
            if (itemId == id) { combo.SelectedIndex = i; return; }
        }
    }

    private async void Save_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        if (string.IsNullOrWhiteSpace(ProductNameBox.Text))
        {
            args.Cancel = true;
            deferral.Complete();
            return;
        }

        var product = _product ?? new Product();
        product.ProductName = ProductNameBox.Text.Trim();
        product.Stock = (int)StockBox.Value;
        product.Unit = SaleUnitCombo.SelectedItem as string ?? "pieces";
        product.SaleUnit = product.Unit;
        var pu = PurchaseUnitCombo.SelectedItem as string;
        product.PurchaseUnit = (string.IsNullOrEmpty(pu) || pu == "Same as sale unit") ? null : pu;
        product.ConversionFactor = (int)ConversionFactorBox.Value;
        product.Price = PriceBox.Value;
        product.WholesalePrice = WholesalePriceBox.Value > 0 ? (decimal)WholesalePriceBox.Value : null;
        product.BuyingPrice = BuyingPriceBox.Value;
        product.Description = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? "No description" : DescriptionBox.Text.Trim();
        product.PStatus = StatusCombo.SelectedIndex == 0 ? "1" : "0";
        product.ExpiryDate = ExpiryDatePicker.Date.Year < 1900 ? null : ExpiryDatePicker.Date.DateTime.Date;
        product.CatId = (CategoryCombo.SelectedItem as Category)?.CatId ?? 0;
        product.BrandId = (BrandCombo.SelectedItem as Brand)?.BrandId ?? 0;
        product.SupplierId = (SupplierCombo.SelectedItem as Supplier)?.SupplierId ?? 0;
        if (product.CatId == 0) product.CatId = null;
        if (product.BrandId == 0) product.BrandId = null;
        if (product.SupplierId == 0) product.SupplierId = null;

        try
        {
            if (_product == null)
                await ServiceLocator.Products.AddAsync(product);
            else
                await ServiceLocator.Products.UpdateAsync(product);
        }
        catch (Exception ex)
        {
            args.Cancel = true;
            var dlg = new ContentDialog { Title = "Error", Content = ex.Message, CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
        }
        deferral.Complete();
    }
}
