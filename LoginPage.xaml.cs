using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class LoginPage : Page
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var email = EmailBox.Text?.Trim() ?? "";
        var password = PasswordBox.Password ?? "";

        ErrorText.Visibility = Visibility.Collapsed;
        LoginButton.IsEnabled = false;

        try
        {
            var (success, _, message) = await ServiceLocator.Auth.LoginAsync(email, password);
            if (success)
            {
                App.MainWindowInstance?.OnLoginSuccess();
            }
            else
            {
                ErrorText.Text = message;
                ErrorText.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            ErrorText.Text = "Connection error: " + ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }
}
