using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using imsapp_desktop.Models;
using imsapp_desktop.ViewModels;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class CategoryListPage : Page
{
    public CategoryListPage()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadCommand.Execute(null);
    }

    private void CategoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryGrid.SelectedItem is Category c)
            ViewModel.SelectedCategory = c;
    }

    private void CategoryGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ViewModel.SelectedCategory != null)
            EditCategory_Click(sender, e);
    }

    private async void AddCategory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CategoryEditDialog(null);
        dialog.XamlRoot = XamlRoot;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.LoadAsync();
    }

    private async void EditCategory_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedCategory == null) return;
        var dialog = new CategoryEditDialog(ViewModel.SelectedCategory);
        dialog.XamlRoot = XamlRoot;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.LoadAsync();
    }

    private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedCategory == null) return;
        var confirm = new ContentDialog
        {
            Title = "Delete Category",
            Content = $"Delete '{ViewModel.SelectedCategory.CategoryName}'?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };
        confirm.XamlRoot = XamlRoot;
        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            await ServiceLocator.Categories.DeleteAsync(ViewModel.SelectedCategory.CatId);
            await ViewModel.LoadAsync();
        }
    }

    private static readonly TableExportService.ExportColumn<Category>[] CategoryColumns =
    {
        new("ID", c => c.CatId.ToString()),
        new("Category Name", c => c.CategoryName),
        new("Status", c => c.StatusDisplay)
    };

    private async void ExportCsv_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Categories", "Categories", ViewModel.Categories.ToList(), CategoryColumns, "csv");
    private async void ExportExcel_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Categories", "Categories", ViewModel.Categories.ToList(), CategoryColumns, "excel");
    private async void ExportPdf_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Categories", "Categories", ViewModel.Categories.ToList(), CategoryColumns, "pdf");
}
