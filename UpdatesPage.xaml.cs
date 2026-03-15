using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Velopack;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class UpdatesPage : Page
{
    private Velopack.UpdateInfo? _pendingUpdate;

    public UpdatesPage()
    {
        InitializeComponent();
    }

    private static string GetCurrentVersion()
    {
        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (!string.IsNullOrEmpty(info?.InformationalVersion))
            return info.InformationalVersion;
        var ver = asm.GetName().Version;
        return ver != null ? ver.ToString() : "—";
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        CurrentVersionText.Text = $"Current version: {GetCurrentVersion()}";
        if (!ServiceLocator.Updates.IsUpdateSupported)
        {
            StatusText.Text = "Automatic updates are not configured. Set Config.AppSettings.UpdateUrl when deploying with Velopack.";
            CheckButton.IsEnabled = false;
        }
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        if (!ServiceLocator.Updates.IsUpdateSupported) return;

        CheckButton.IsEnabled = false;
        ProgressBar.Visibility = Visibility.Visible;
        StatusText.Text = "Checking for updates...";
        UpdateButton.Visibility = Visibility.Collapsed;
        _pendingUpdate = null;

        try
        {
            var update = await ServiceLocator.Updates.CheckForUpdatesAsync();
            if (update != null)
            {
                _pendingUpdate = update;
                var version = update.TargetFullRelease?.Version.ToString() ?? "new";
                StatusText.Text = $"Update available: version {version}. Click 'Install Update' to download and install.";
                UpdateButton.Visibility = Visibility.Visible;
                UpdateButton.IsEnabled = true;
            }
            else
            {
                StatusText.Text = "You're running the latest version. No updates available.";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Could not check for updates: {ex.Message}";
        }
        finally
        {
            CheckButton.IsEnabled = true;
            ProgressBar.Visibility = Visibility.Collapsed;
        }
    }

    private async void InstallUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingUpdate == null || !ServiceLocator.Updates.IsUpdateSupported) return;

        UpdateButton.IsEnabled = false;
        ProgressBar.Visibility = Visibility.Visible;
        StatusText.Text = "Downloading update...";

        try
        {
            await ServiceLocator.Updates.DownloadUpdatesAsync(_pendingUpdate);
            StatusText.Text = "Update downloaded. Restarting to apply...";
            ServiceLocator.Updates.ApplyUpdatesAndRestart(_pendingUpdate);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Update failed: {ex.Message}";
            UpdateButton.IsEnabled = true;
        }
        finally
        {
            ProgressBar.Visibility = Visibility.Collapsed;
        }
    }
}
