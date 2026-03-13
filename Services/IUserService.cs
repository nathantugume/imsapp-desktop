using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public interface IUserService
{
    Task<IReadOnlyList<User>> GetAllUsersAsync();
    Task<User?> GetByIdAsync(int id);
    Task<(bool Success, string Message)> CreateAsync(string name, string email, string password, string country);
    Task<(bool Success, string Message)> UpdateAsync(int id, string name, string email, string country);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
}
