using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class ResetPasswordDialog : ContentDialog
{
    private readonly int _userId;

    public ResetPasswordDialog(int userId)
    {
        InitializeComponent();
        _userId = userId;
    }

    private async void Reset_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        var oldPwd = OldPasswordBox.Password;
        var newPwd = NewPasswordBox.Password;
        var confirmPwd = ConfirmPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(oldPwd) || string.IsNullOrWhiteSpace(newPwd) || string.IsNullOrWhiteSpace(confirmPwd))
        {
            args.Cancel = true;
            var dlg = new ContentDialog { Title = "Validation", Content = "All fields are required.", CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
            deferral.Complete();
            return;
        }

        if (newPwd.Length < 6)
        {
            args.Cancel = true;
            var dlg = new ContentDialog { Title = "Validation", Content = "New password must be at least 6 characters.", CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
            deferral.Complete();
            return;
        }

        if (newPwd != confirmPwd)
        {
            args.Cancel = true;
            var dlg = new ContentDialog { Title = "Validation", Content = "New password and confirmation do not match.", CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
            deferral.Complete();
            return;
        }

        try
        {
            var (success, message) = await ServiceLocator.Auth.ResetPasswordAsync(_userId, oldPwd, newPwd);
            if (!success)
            {
                args.Cancel = true;
                var errDlg = new ContentDialog { Title = "Error", Content = message, CloseButtonText = "OK" };
                errDlg.XamlRoot = XamlRoot;
                await errDlg.ShowAsync();
            }
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
