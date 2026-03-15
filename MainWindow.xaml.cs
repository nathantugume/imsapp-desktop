using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using imsapp_desktop.Services;

namespace imsapp_desktop
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Loading overlay is visible; MainContent hidden until OnBootstrapComplete
        }

        public IProgress<DatabaseBootstrapProgress> GetBootstrapProgress()
        {
            return new Progress<DatabaseBootstrapProgress>(p =>
            {
                LoadingStatus.Text = p.Status;
                LoadingProgress.IsIndeterminate = p.IsIndeterminate;
                LoadingProgress.Value = p.ProgressPercent;
            });
        }

        public void OnBootstrapComplete(DatabaseBootstrapService.BootstrapResult result, string message)
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;

            if (result == DatabaseBootstrapService.BootstrapResult.Failed)
            {
                ShowLogin(); // Show login so user can use "Set up database manually"
                _ = ShowDatabaseErrorAsync(message);
                return;
            }

            if (ServiceLocator.Auth.IsLoggedIn)
                ShowMainApp();
            else
                ShowLogin();
        }

        private async System.Threading.Tasks.Task ShowDatabaseErrorAsync(string message)
        {
            var xamlRoot = Content?.XamlRoot;
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
        }

        public void OnLoginSuccess()
        {
            ShowMainApp();
        }

        private void ShowLogin()
        {
            NavView.Visibility = Visibility.Collapsed;
            LoginFrame.Visibility = Visibility.Visible;
            LoginFrame.Navigate(typeof(LoginPage));
        }

        private async void ShowMainApp()
        {
            LoginFrame.Visibility = Visibility.Collapsed;
            NavView.Visibility = Visibility.Visible;
            UserManagementItem.Visibility = ServiceLocator.Auth.CurrentUser?.Role == "Master" ? Visibility.Visible : Visibility.Collapsed;
            NavView.SelectedItem = NavView.MenuItems.First();
            await ServiceLocator.Branding.LoadAsync();
            ContentFrame.Navigate(typeof(DashboardPage));
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
            {
                if (tag == "Logout")
                {
                    ServiceLocator.Auth.Logout();
                    ShowLogin();
                    return;
                }
                Type pageType = tag switch
                {
                    "Dashboard" => typeof(DashboardPage),
                    "Products" => typeof(ProductListPage),
                    "Orders" => typeof(OrderListPage),
                    "Categories" => typeof(CategoryListPage),
                    "Brands" => typeof(BrandListPage),
                    "Suppliers" => typeof(SupplierListPage),
                    "Stock" => typeof(StockReconciliationPage),
                    "Payments" => typeof(CustomerPaymentsPage),
                    "Reports" => typeof(ReportsPage),
                    "Users" => typeof(UserListPage),
                    "Settings" => typeof(SettingsPage),
                    "Updates" => typeof(UpdatesPage),
                    "Profile" => typeof(ProfilePage),
                    _ => typeof(PlaceholderPage)
                };
                ContentFrame.Navigate(pageType);
            }
        }
    }
}
