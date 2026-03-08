using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop.ViewModels;

public partial class CustomerPaymentsViewModel : ObservableObject
{
    private readonly ICustomerPaymentService _service = ServiceLocator.CustomerPayments;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private OutstandingOrder? _selectedOrder;

    public ObservableCollection<OutstandingOrder> OutstandingOrders { get; } = new();
    public ObservableCollection<CustomerPayment> PaymentHistory { get; } = new();
    public bool HasSelection => SelectedOrder != null;

    partial void OnSelectedOrderChanged(OutstandingOrder? value) => OnPropertyChanged(nameof(HasSelection));

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var orders = await _service.GetOutstandingOrdersAsync();
            var history = await _service.GetPaymentHistoryAsync();

            OutstandingOrders.Clear();
            foreach (var o in orders)
                OutstandingOrders.Add(o);

            PaymentHistory.Clear();
            foreach (var p in history)
                PaymentHistory.Add(p);
        }
        catch (System.Exception ex)
        {
            ErrorMessage = "Failed to load: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
