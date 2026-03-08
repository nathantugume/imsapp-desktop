using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class BrandEditDialog : ContentDialog
{
    private readonly Brand? _brand;

    public BrandEditDialog(Brand? brand)
    {
        InitializeComponent();
        _brand = brand;
        Title = brand == null ? "Add Brand" : "Edit Brand";

        if (brand != null)
        {
            BrandNameBox.Text = brand.BrandName;
            StatusCombo.SelectedIndex = brand.BStatus == "1" ? 0 : 1;
        }
    }

    private async void Save_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        if (string.IsNullOrWhiteSpace(BrandNameBox.Text))
        {
            args.Cancel = true;
            deferral.Complete();
            return;
        }

        var b = _brand ?? new Brand();
        b.BrandName = BrandNameBox.Text.Trim();
        b.BStatus = StatusCombo.SelectedIndex == 0 ? "1" : "0";

        try
        {
            if (_brand == null)
                await ServiceLocator.Brands.AddAsync(b);
            else
                await ServiceLocator.Brands.UpdateAsync(b);
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
