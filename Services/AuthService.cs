using System.Threading.Tasks;
using Dapper;
using imsapp_desktop.Data;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public class AuthService : IAuthService
{
    public bool IsLoggedIn => CurrentUser != null;
    public User? CurrentUser { get; private set; }

    public async Task<(bool Success, User? User, string Message)> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (false, null, "Email and password are required.");

        const string sql = "SELECT id, name, email, password, role, status FROM users WHERE email = @Email LIMIT 1";
        using var conn = DatabaseFactory.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { Email = email.Trim() });

        if (row == null)
            return (false, null, "Invalid email or password.");

        string? storedHash = (string?)row.password;
        bool passwordMatch = false;
        if (!string.IsNullOrEmpty(storedHash))
        {
            if (storedHash.StartsWith("$2") && BCrypt.Net.BCrypt.Verify(password, storedHash))
                passwordMatch = true;
            else if (password == storedHash)
                passwordMatch = true; // Plain text fallback (PHP compatibility)
        }

        if (!passwordMatch)
            return (false, null, "Invalid email or password.");

        if ((string?)row.status == "0")
            return (false, null, "Account is disabled. Contact administrator.");

        var user = new User
        {
            Id = (int)row.id,
            Name = (string?)row.name,
            Email = (string?)row.email,
            Role = (string?)row.role,
            Status = (string?)row.status ?? "1"
        };
        CurrentUser = user;
        return (true, user, "Login successful.");
    }

    public void Logout()
    {
        CurrentUser = null;
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(int userId, string oldPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            return (false, "All fields are required.");

        if (newPassword.Length < 6)
            return (false, "New password must be at least 6 characters.");

        const string sql = "SELECT id, password FROM users WHERE id = @Id LIMIT 1";
        using var conn = DatabaseFactory.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = userId });
        if (row == null)
            return (false, "User not found.");

        string? storedHash = (string?)row.password;
        bool passwordMatch = false;
        if (!string.IsNullOrEmpty(storedHash))
        {
            if (storedHash.StartsWith("$2") && BCrypt.Net.BCrypt.Verify(oldPassword, storedHash))
                passwordMatch = true;
            else if (oldPassword == storedHash)
                passwordMatch = true;
        }

        if (!passwordMatch)
            return (false, "Old password does not match.");

        var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        const string updateSql = "UPDATE users SET password = @Password WHERE id = @Id";
        var affected = await conn.ExecuteAsync(updateSql, new { Password = newHash, Id = userId });
        if (affected == 0)
            return (false, "Password could not be updated.");

        return (true, "Password has been reset successfully.");
    }
}
