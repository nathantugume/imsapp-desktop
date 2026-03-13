using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop.ViewModels;

public partial class OrderListViewModel : ObservableObject
{
    private readonly IOrderService _orderService = ServiceLocator.Orders;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private Order? _selectedOrder;

    public ObservableCollection<Order> Orders { get; } = new();
    public bool HasSelection => SelectedOrder != null;

    partial void OnSelectedOrderChanged(Order? value) => OnPropertyChanged(nameof(HasSelection));

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var list = await _orderService.GetAllAsync();
            Orders.Clear();
            foreach (var o in list)
                Orders.Add(o);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to load orders: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
