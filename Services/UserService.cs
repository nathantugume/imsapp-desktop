using System.Security.Cryptography;
using Dapper;
using imsapp_desktop.Data;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public class UserService : IUserService
{
    public async Task<IReadOnlyList<User>> GetAllUsersAsync()
    {
        const string sql = "SELECT id AS Id, name AS Name, email AS Email, role AS Role, status AS Status, vcode AS Vcode, country AS Country FROM users WHERE role = 'User' ORDER BY name";
        using var conn = DatabaseFactory.CreateConnection();
        var list = await conn.QueryAsync<User>(sql);
        return list.ToList();
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        const string sql = "SELECT id AS Id, name AS Name, email AS Email, role AS Role, status AS Status, vcode AS Vcode, country AS Country FROM users WHERE id = @Id";
        using var conn = DatabaseFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<(bool Success, string Message)> CreateAsync(string name, string email, string password, string country)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(country))
            return (false, "All fields are required.");

        if (password.Length < 6)
            return (false, "Password must be at least 6 characters.");

        if (await EmailExistsAsync(email))
            return (false, "Email address already exists.");

        var vcode = GenerateVcode();
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        const string sql = @"INSERT INTO users (name, email, password, role, vcode, country, status)
            VALUES (@Name, @Email, @Password, 'User', @Vcode, @Country, '1')";
        using var conn = DatabaseFactory.CreateConnection();
        await conn.ExecuteAsync(sql, new { Name = name.Trim(), Email = email.Trim().ToLowerInvariant(), Password = hashedPassword, Vcode = vcode, Country = country.Trim() });
        return (true, "User created successfully.");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(int id, string name, string email, string country)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(country))
            return (false, "All fields are required.");

        if (await EmailExistsAsync(email, id))
            return (false, "Email address already exists.");

        const string sql = "UPDATE users SET name = @Name, email = @Email, country = @Country WHERE id = @Id AND role = 'User'";
        using var conn = DatabaseFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { Name = name.Trim(), Email = email.Trim().ToLowerInvariant(), Country = country.Trim(), Id = id });
        if (rows == 0)
            return (false, "User not found or cannot be updated.");
        return (true, "User updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM users WHERE id = @Id AND role = 'User'";
        using var conn = DatabaseFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { Id = id });
        if (rows == 0)
            return (false, "User not found or cannot be deleted.");
        return (true, "User deleted successfully.");
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        string sql;
        object param;
        if (excludeId.HasValue)
        {
            sql = "SELECT 1 FROM users WHERE LOWER(email) = @Email AND id != @ExcludeId LIMIT 1";
            param = new { Email = email.Trim().ToLowerInvariant(), ExcludeId = excludeId.Value };
        }
        else
        {
            sql = "SELECT 1 FROM users WHERE LOWER(email) = @Email LIMIT 1";
            param = new { Email = email.Trim().ToLowerInvariant() };
        }
        using var conn = DatabaseFactory.CreateConnection();
        var exists = await conn.ExecuteScalarAsync<int?>(sql, param);
        return exists.HasValue;
    }

    private static string GenerateVcode()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var bytes = new byte[12];
        RandomNumberGenerator.Fill(bytes);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}
