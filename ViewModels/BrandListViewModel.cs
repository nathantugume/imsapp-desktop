using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop.ViewModels;

public partial class BrandListViewModel : ObservableObject
{
    private readonly IBrandService _brandService = ServiceLocator.Brands;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private Brand? _selectedBrand;

    public ObservableCollection<Brand> Brands { get; } = new();
    public bool HasSelection => SelectedBrand != null;

    partial void OnSelectedBrandChanged(Brand? value) => OnPropertyChanged(nameof(HasSelection));

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var list = await _brandService.GetAllAsync();
            Brands.Clear();
            foreach (var b in list)
                Brands.Add(b);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to load brands: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
