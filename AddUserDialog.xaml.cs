using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class AddUserDialog : ContentDialog
{
    public AddUserDialog()
    {
        InitializeComponent();
    }

    private async void Create_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        var name = NameBox.Text?.Trim() ?? "";
        var email = EmailBox.Text?.Trim() ?? "";
        var password = PasswordBox.Password;
        var confirmPassword = ConfirmPasswordBox.Password;
        var country = CountryBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(country))
        {
            args.Cancel = true;
            var dlg = new ContentDialog { Title = "Error", Content = "All fields are required.", CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
            deferral.Complete();
            return;
        }

        if (password.Length < 6)
        {
            args.Cancel = true;
            var dlg = new ContentDialog { Title = "Error", Content = "Password must be at least 6 characters.", CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
            deferral.Complete();
            return;
        }

        if (password != confirmPassword)
        {
            args.Cancel = true;
            var dlg = new ContentDialog { Title = "Error", Content = "Passwords do not match.", CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
            deferral.Complete();
            return;
        }

        var (success, message) = await ServiceLocator.Users.CreateAsync(name, email, password, country);
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
