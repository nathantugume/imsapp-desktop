using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ServiceLocator.Branding.LoadAsync();
        var s = ServiceLocator.Branding.Current;
        BusinessNameBox.Text = s.BusinessName;
        BusinessNameShortBox.Text = s.BusinessNameShort;
        TaglineBox.Text = s.BusinessTagline;
        AddressBox.Text = s.BusinessAddress;
        PhoneBox.Text = s.BusinessPhone;
        EmailBox.Text = s.BusinessEmail;
        CurrencySymbolBox.Text = s.CurrencySymbol;
        LowStockBox.Value = s.LowStockThreshold;
        CriticalStockBox.Value = s.CriticalStockThreshold;
        ExpiryWarningBox.Value = s.ExpiryWarningDays;
        ExpiryCriticalBox.Value = s.ExpiryCriticalDays;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(BusinessNameBox.Text) || string.IsNullOrWhiteSpace(BusinessNameShortBox.Text))
        {
            var dlg = new ContentDialog { Title = "Validation", Content = "Business name (full and short) are required.", CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
            return;
        }

        var settings = new BrandingSettings
        {
            BusinessName = BusinessNameBox.Text.Trim(),
            BusinessNameShort = BusinessNameShortBox.Text.Trim(),
            BusinessTagline = TaglineBox.Text.Trim(),
            BusinessAddress = AddressBox.Text.Trim(),
            BusinessPhone = PhoneBox.Text.Trim(),
            BusinessEmail = EmailBox.Text.Trim(),
            CurrencySymbol = string.IsNullOrWhiteSpace(CurrencySymbolBox.Text) ? "ugx" : CurrencySymbolBox.Text.Trim().ToLowerInvariant(),
            LowStockThreshold = (int)LowStockBox.Value,
            CriticalStockThreshold = (int)CriticalStockBox.Value,
            ExpiryWarningDays = (int)ExpiryWarningBox.Value,
            ExpiryCriticalDays = (int)ExpiryCriticalBox.Value
        };

        try
        {
            await ServiceLocator.Branding.SaveAsync(settings);
            var dlg = new ContentDialog { Title = "Saved", Content = "Settings saved successfully. Changes apply across the app.", CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
        }
        catch (Exception ex)
        {
            var dlg = new ContentDialog { Title = "Error", Content = "Failed to save: " + ex.Message, CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // Reload from saved (discard edits)
        Page_Loaded(sender, e);
    }
}
