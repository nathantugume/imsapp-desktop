using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using imsapp_desktop.Data;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public class CategoryService : ICategoryService
{
    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        const string sql = "SELECT cat_id AS CatId, main_cat AS MainCat, category_name AS CategoryName, status AS Status, created_at AS CreatedAt FROM categories ORDER BY category_name";
        using var conn = DatabaseFactory.CreateConnection();
        var list = await conn.QueryAsync<Category>(sql);
        return list.ToList();
    }

    public async Task<Category?> GetByIdAsync(int catId)
    {
        const string sql = "SELECT cat_id AS CatId, main_cat AS MainCat, category_name AS CategoryName, status AS Status, created_at AS CreatedAt FROM categories WHERE cat_id = @CatId";
        using var conn = DatabaseFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Category>(sql, new { CatId = catId });
    }

    public async Task<int> AddAsync(Category category)
    {
        const string sql = "INSERT INTO categories (main_cat, category_name, status) VALUES (@MainCat, @CategoryName, @Status); SELECT LAST_INSERT_ID();";
        using var conn = DatabaseFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, category);
    }

    public async Task<bool> UpdateAsync(Category category)
    {
        const string sql = "UPDATE categories SET category_name=@CategoryName, status=@Status WHERE cat_id=@CatId";
        using var conn = DatabaseFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, category);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int catId)
    {
        const string sql = "DELETE FROM categories WHERE cat_id = @CatId";
        using var conn = DatabaseFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { CatId = catId });
        return rows > 0;
    }
}
