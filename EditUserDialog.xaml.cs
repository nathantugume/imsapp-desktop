using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class EditUserDialog : ContentDialog
{
    private readonly User _user;

    public EditUserDialog(User user)
    {
        InitializeComponent();
        _user = user;
        NameBox.Text = user.Name ?? "";
        EmailBox.Text = user.Email ?? "";
        CountryBox.Text = user.Country ?? "";
    }

    private async void Update_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        var name = NameBox.Text?.Trim() ?? "";
        var email = EmailBox.Text?.Trim() ?? "";
        var country = CountryBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(country))
        {
            args.Cancel = true;
            var dlg = new ContentDialog { Title = "Error", Content = "All fields are required.", CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
            deferral.Complete();
            return;
        }

        var (success, message) = await ServiceLocator.Users.UpdateAsync(_user.Id, name, email, country);
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
