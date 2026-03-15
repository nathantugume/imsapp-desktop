using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PdfSharp.Fonts;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using imsapp_desktop.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace imsapp_desktop
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private static Window? s_window;

        public static MainWindow? MainWindowInstance => s_window as MainWindow;

        public App()
        {
            Services.CrashLog.Write("App constructor: before InitializeComponent");
            InitializeComponent();
            Services.CrashLog.Write("App constructor: after InitializeComponent");
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
            Services.CrashLog.Write("App constructor: done");
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Services.CrashLog.Write("OnLaunched: start");

            var statusText = new TextBlock
            {
                Text = "Checking database connection...",
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16)
            };
            var progressBar = new ProgressBar
            {
                Height = 8,
                IsIndeterminate = true,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            var loadingPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(32, 24, 32, 24),
                Children = { statusText, progressBar }
            };
            var loadingWindow = new Window
            {
                Title = "IMS App",
                Content = loadingPanel,
                ExtendsContentIntoTitleBar = true
            };
            loadingWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32(420, 160));
            loadingWindow.Activate();

            var progress = new Progress<Services.DatabaseBootstrapProgress>(p =>
            {
                var queue = DispatcherQueue.GetForCurrentThread();
                queue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    statusText.Text = p.Status;
                    progressBar.IsIndeterminate = p.IsIndeterminate;
                    progressBar.Value = p.ProgressPercent;
                });
            });

            var (result, message) = await DatabaseBootstrapService.EnsureDatabaseAsync(progress);
            Services.CrashLog.Write($"OnLaunched: database result={result}");

            loadingWindow.Close();

            if (result == DatabaseBootstrapService.BootstrapResult.Failed)
            {
                s_window = new MainWindow();
                s_window.Activate();

                var queue = DispatcherQueue.GetForCurrentThread();
                queue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
                {
                    var xamlRoot = s_window?.Content?.XamlRoot;
                    if (xamlRoot != null)
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "Database Error",
                            Content = message,
                            PrimaryButtonText = "OK",
                            XamlRoot = xamlRoot
                        };
                        await dialog.ShowAsync();
                    }
                });
                return;
            }

            s_window = new MainWindow();
            s_window.Activate();
        }
    }
}
