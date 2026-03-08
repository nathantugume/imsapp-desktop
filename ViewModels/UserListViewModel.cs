using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop.ViewModels;

public partial class UserListViewModel : ObservableObject
{
    private readonly IUserService _service = ServiceLocator.Users;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private User? _selectedUser;

    public ObservableCollection<User> Users { get; } = new();
    public bool HasSelection => SelectedUser != null;

    partial void OnSelectedUserChanged(User? value) => OnPropertyChanged(nameof(HasSelection));

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var list = await _service.GetAllUsersAsync();
            Users.Clear();
            foreach (var u in list)
                Users.Add(u);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to load users: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
