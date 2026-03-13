using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class ProfilePage : Page
{
    public ProfilePage()
    {
        InitializeComponent();
        Loaded += ProfilePage_Loaded;
    }

    private void ProfilePage_Loaded(object sender, RoutedEventArgs e)
    {
        var user = ServiceLocator.Auth.CurrentUser;
        if (user != null)
        {
            NameText.Text = user.Name ?? "—";
            EmailText.Text = user.Email ?? "—";
            RoleText.Text = user.Role ?? "—";
        }
    }

    private async void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        var user = ServiceLocator.Auth.CurrentUser;
        if (user == null) return;

        var dialog = new ResetPasswordDialog(user.Id);
        dialog.XamlRoot = XamlRoot;
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var dlg = new ContentDialog { Title = "Success", Content = "Password has been reset successfully.", CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
        }
    }
}
