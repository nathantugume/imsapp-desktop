using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class CreateReconciliationDialog : ContentDialog
{
    public CreateReconciliationDialog()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            await LoadProducts();
            ProductCombo.SelectionChanged += (s, _) => UpdateSystemStock();
            UpdateSystemStock();
        };
    }

    private async System.Threading.Tasks.Task LoadProducts()
    {
        var products = await ServiceLocator.StockReconciliation.GetProductsForReconciliationAsync();
        ProductCombo.Items.Clear();
        foreach (var p in products)
            ProductCombo.Items.Add(p);
        if (ProductCombo.Items.Count > 0)
            ProductCombo.SelectedIndex = 0;
    }

    private void UpdateSystemStock()
    {
        SystemStockText.Text = ProductCombo.SelectedItem is Product p ? $"System stock: {p.Stock}" : "System stock: -";
    }

    private async void Create_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        if (ProductCombo.SelectedItem is not Product product)
        {
            args.Cancel = true;
            deferral.Complete();
            return;
        }

        var physicalCount = (int)PhysicalCountBox.Value;
        var (success, message) = await ServiceLocator.StockReconciliation.CreateAsync(product.Pid, physicalCount, NotesBox.Text?.Trim());

        if (!success)
        {
            args.Cancel = true;
            var dlg = new ContentDialog { Title = "Error", Content = message, CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
        }
        deferral.Complete();
    }
}
