using System.Threading.Tasks;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public interface IAuthService
{
    Task<(bool Success, User? User, string Message)> LoginAsync(string email, string password);
    Task<(bool Success, string Message)> ResetPasswordAsync(int userId, string oldPassword, string newPassword);
    bool IsLoggedIn { get; }
    User? CurrentUser { get; }
    void Logout();
}
