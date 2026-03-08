using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using imsapp_desktop.Data;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public class BrandService : IBrandService
{
    public async Task<IReadOnlyList<Brand>> GetAllAsync()
    {
        const string sql = "SELECT brand_id AS BrandId, brand_name AS BrandName, b_status AS BStatus, created_at AS CreatedAt, updated_at AS UpdatedAt FROM brands ORDER BY brand_name";
        using var conn = DatabaseFactory.CreateConnection();
        var list = await conn.QueryAsync<Brand>(sql);
        return list.ToList();
    }

    public async Task<Brand?> GetByIdAsync(int brandId)
    {
        const string sql = "SELECT brand_id AS BrandId, brand_name AS BrandName, b_status AS BStatus, created_at AS CreatedAt, updated_at AS UpdatedAt FROM brands WHERE brand_id = @BrandId";
        using var conn = DatabaseFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Brand>(sql, new { BrandId = brandId });
    }

    public async Task<int> AddAsync(Brand brand)
    {
        const string sql = "INSERT INTO brands (brand_name, b_status) VALUES (@BrandName, @BStatus); SELECT LAST_INSERT_ID();";
        using var conn = DatabaseFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, brand);
    }

    public async Task<bool> UpdateAsync(Brand brand)
    {
        const string sql = "UPDATE brands SET brand_name=@BrandName, b_status=@BStatus WHERE brand_id=@BrandId";
        using var conn = DatabaseFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, brand);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int brandId)
    {
        const string sql = "DELETE FROM brands WHERE brand_id = @BrandId";
        using var conn = DatabaseFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { BrandId = brandId });
        return rows > 0;
    }
}
