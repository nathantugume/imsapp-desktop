using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop.ViewModels;

public partial class CategoryListViewModel : ObservableObject
{
    private readonly ICategoryService _categoryService = ServiceLocator.Categories;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private Category? _selectedCategory;

    public ObservableCollection<Category> Categories { get; } = new();
    public bool HasSelection => SelectedCategory != null;

    partial void OnSelectedCategoryChanged(Category? value) => OnPropertyChanged(nameof(HasSelection));

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var list = await _categoryService.GetAllAsync();
            Categories.Clear();
            foreach (var c in list)
                Categories.Add(c);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to load categories: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
