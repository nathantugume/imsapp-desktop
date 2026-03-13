using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using imsapp_desktop.Models;
using imsapp_desktop.Services;
using imsapp_desktop.ViewModels;

namespace imsapp_desktop;

public sealed partial class UserListPage : Page
{
    public UserListPage()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadCommand.Execute(null);
    }

    private void UserGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UserGrid.SelectedItem is User u)
            ViewModel.SelectedUser = u;
    }

    private void UserGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ViewModel.SelectedUser != null)
            EditUser_Click(sender, e);
    }

    private async void AddUser_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddUserDialog();
        dialog.XamlRoot = XamlRoot;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.LoadAsync();
    }

    private async void EditUser_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedUser == null) return;
        var dialog = new EditUserDialog(ViewModel.SelectedUser);
        dialog.XamlRoot = XamlRoot;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.LoadAsync();
    }

    private async void DeleteUser_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedUser == null) return;
        var confirm = new ContentDialog
        {
            Title = "Delete User",
            Content = $"Delete user '{ViewModel.SelectedUser.Name}'?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };
        confirm.XamlRoot = XamlRoot;
        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            var (success, msg) = await ServiceLocator.Users.DeleteAsync(ViewModel.SelectedUser.Id);
            var dlg = new ContentDialog { Title = success ? "Success" : "Error", Content = msg, CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
            if (success)
                await ViewModel.LoadAsync();
        }
    }

    private static readonly TableExportService.ExportColumn<User>[] UserColumns =
    {
        new("ID", u => u.Id.ToString()),
        new("Name", u => u.Name ?? ""),
        new("Email", u => u.Email ?? ""),
        new("Role", u => u.Role ?? ""),
        new("Country", u => u.Country ?? ""),
        new("Status", u => u.StatusDisplay)
    };

    private async void ExportCsv_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Users", "Users", ViewModel.Users.ToList(), UserColumns, "csv");
    private async void ExportExcel_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Users", "Users", ViewModel.Users.ToList(), UserColumns, "excel");
    private async void ExportPdf_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Users", "Users", ViewModel.Users.ToList(), UserColumns, "pdf");
}
