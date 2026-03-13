using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class CategoryEditDialog : ContentDialog
{
    private readonly Category? _category;

    public CategoryEditDialog(Category? category)
    {
        InitializeComponent();
        _category = category;
        Title = category == null ? "Add Category" : "Edit Category";

        if (category != null)
        {
            CategoryNameBox.Text = category.CategoryName;
            StatusCombo.SelectedIndex = category.Status == "1" ? 0 : 1;
        }
    }

    private async void Save_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        if (string.IsNullOrWhiteSpace(CategoryNameBox.Text))
        {
            args.Cancel = true;
            deferral.Complete();
            return;
        }

        var cat = _category ?? new Category();
        cat.CategoryName = CategoryNameBox.Text.Trim();
        cat.Status = StatusCombo.SelectedIndex == 0 ? "1" : "0";

        try
        {
            if (_category == null)
                await ServiceLocator.Categories.AddAsync(cat);
            else
                await ServiceLocator.Categories.UpdateAsync(cat);
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
