using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop.ViewModels;

public partial class StockReconciliationListViewModel : ObservableObject
{
    private readonly IStockReconciliationService _service = ServiceLocator.StockReconciliation;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private StockReconciliation? _selectedReconciliation;

    public ObservableCollection<StockReconciliation> Reconciliations { get; } = new();
    public bool HasSelection => SelectedReconciliation != null;
    public bool CanApproveReject => SelectedReconciliation?.Status == "pending";

    partial void OnSelectedReconciliationChanged(StockReconciliation? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(CanApproveReject));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var list = await _service.GetAllAsync();
            Reconciliations.Clear();
            foreach (var r in list)
                Reconciliations.Add(r);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to load: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ApproveAsync()
    {
        if (SelectedReconciliation == null || SelectedReconciliation.Status != "pending") return;
        var (success, message) = await _service.ApproveAsync(SelectedReconciliation.Id);
        if (success)
            await LoadAsync();
        else
            ErrorMessage = message;
    }

    [RelayCommand]
    public async Task RejectAsync()
    {
        if (SelectedReconciliation == null || SelectedReconciliation.Status != "pending") return;
        var (success, message) = await _service.RejectAsync(SelectedReconciliation.Id);
        if (success)
            await LoadAsync();
        else
            ErrorMessage = message;
    }
}
