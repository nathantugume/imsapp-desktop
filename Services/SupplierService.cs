using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using imsapp_desktop.Data;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public class SupplierService : ISupplierService
{
    public async Task<IReadOnlyList<Supplier>> GetAllAsync()
    {
        const string sql = "SELECT supplier_id AS SupplierId, supplier_name AS SupplierName, contact_person AS ContactPerson, phone AS Phone, email AS Email, address AS Address, status AS Status, created_at AS CreatedAt, updated_at AS UpdatedAt FROM suppliers ORDER BY supplier_name";
        using var conn = DatabaseFactory.CreateConnection();
        var list = await conn.QueryAsync<Supplier>(sql);
        return list.ToList();
    }

    public async Task<Supplier?> GetByIdAsync(int supplierId)
    {
        const string sql = "SELECT supplier_id AS SupplierId, supplier_name AS SupplierName, contact_person AS ContactPerson, phone AS Phone, email AS Email, address AS Address, status AS Status, created_at AS CreatedAt, updated_at AS UpdatedAt FROM suppliers WHERE supplier_id = @SupplierId";
        using var conn = DatabaseFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Supplier>(sql, new { SupplierId = supplierId });
    }

    public async Task<int> AddAsync(Supplier supplier)
    {
        const string sql = @"INSERT INTO suppliers (supplier_name, contact_person, phone, email, address, status)
            VALUES (@SupplierName, @ContactPerson, @Phone, @Email, @Address, @Status);
            SELECT LAST_INSERT_ID();";
        using var conn = DatabaseFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, supplier);
    }

    public async Task<bool> UpdateAsync(Supplier supplier)
    {
        const string sql = @"UPDATE suppliers SET supplier_name=@SupplierName, contact_person=@ContactPerson,
            phone=@Phone, email=@Email, address=@Address, status=@Status WHERE supplier_id=@SupplierId";
        using var conn = DatabaseFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, supplier);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int supplierId)
    {
        const string sql = "DELETE FROM suppliers WHERE supplier_id = @SupplierId";
        using var conn = DatabaseFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { SupplierId = supplierId });
        return rows > 0;
    }
}
