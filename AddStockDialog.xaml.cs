using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class AddStockDialog : ContentDialog
{
    private readonly Product _product;

    public AddStockDialog(Product product)
    {
        InitializeComponent();
        _product = product;
        ProductNameText.Text = product.ProductName;
        CurrentStockText.Text = $"Current stock: {product.Stock}";
    }

    private async void Add_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        var qty = (int)QuantityBox.Value;
        if (qty <= 0)
        {
            args.Cancel = true;
            var dlg = new ContentDialog { Title = "Validation", Content = "Quantity must be at least 1.", CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
            deferral.Complete();
            return;
        }

        var (success, message) = await ServiceLocator.Products.AddStockAsync(_product.Pid, qty);
        if (!success)
        {
            args.Cancel = true;
            var errDlg = new ContentDialog { Title = "Error", Content = message, CloseButtonText = "OK" };
            errDlg.XamlRoot = XamlRoot;
            await errDlg.ShowAsync();
        }
        deferral.Complete();
    }
}
