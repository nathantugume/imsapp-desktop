using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop.ViewModels;

public partial class SupplierListViewModel : ObservableObject
{
    private readonly ISupplierService _supplierService = ServiceLocator.Suppliers;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private Supplier? _selectedSupplier;

    public ObservableCollection<Supplier> Suppliers { get; } = new();
    public bool HasSelection => SelectedSupplier != null;

    partial void OnSelectedSupplierChanged(Supplier? value) => OnPropertyChanged(nameof(HasSelection));

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var list = await _supplierService.GetAllAsync();
            Suppliers.Clear();
            foreach (var s in list)
                Suppliers.Add(s);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to load suppliers: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
