using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using imsapp_desktop.Data;
using imsapp_desktop.Models;
using MySqlConnector;

namespace imsapp_desktop.Services;

public class ProductService : IProductService
{
    // Full query (schema with supplier_id, wholesale_price, unit, etc.)
    private const string GetAllSqlFull = @"
        SELECT p.pid AS Pid, p.cat_id AS CatId, p.brand_id AS BrandId, p.supplier_id AS SupplierId,
            p.product_name AS ProductName, p.stock AS Stock, p.price AS Price, p.wholesale_price AS WholesalePrice,
            p.unit AS Unit, p.purchase_unit AS PurchaseUnit, p.sale_unit AS SaleUnit,
            p.conversion_factor AS ConversionFactor, p.unit_cost AS UnitCost, p.buying_price AS BuyingPrice,
            p.description AS Description, p.p_status AS PStatus, p.created_at AS CreatedAt, p.expiry_date AS ExpiryDate,
            c.category_name AS CategoryName, b.brand_name AS BrandName, s.supplier_name AS SupplierName
        FROM products p
        LEFT JOIN categories c ON p.cat_id = c.cat_id
        LEFT JOIN brands b ON p.brand_id = b.brand_id
        LEFT JOIN suppliers s ON p.supplier_id = s.supplier_id
        ORDER BY p.pid DESC";

    // Minimal query (base schema: no supplier_id, wholesale_price, unit, unit_cost)
    private const string GetAllSqlMinimal = @"
        SELECT p.pid AS Pid, p.cat_id AS CatId, p.brand_id AS BrandId,
            p.product_name AS ProductName, p.stock AS Stock, p.price AS Price, p.buying_price AS BuyingPrice,
            p.description AS Description, p.p_status AS PStatus, p.created_at AS CreatedAt, p.expiry_date AS ExpiryDate,
            c.category_name AS CategoryName, b.brand_name AS BrandName
        FROM products p
        LEFT JOIN categories c ON p.cat_id = c.cat_id
        LEFT JOIN brands b ON p.brand_id = b.brand_id
        ORDER BY p.pid DESC";

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        using var conn = DatabaseFactory.CreateConnection();
        try
        {
            var list = await conn.QueryAsync<Product>(GetAllSqlFull);
            return list.ToList();
        }
        catch (MySqlException ex) when (IsUnknownColumn(ex))
        {
            var list = await conn.QueryAsync<Product>(GetAllSqlMinimal);
            return list.ToList();
        }
    }

    public async Task<Product?> GetByIdAsync(int pid)
    {
        const string sqlFull = "SELECT pid AS Pid, cat_id AS CatId, brand_id AS BrandId, supplier_id AS SupplierId, product_name AS ProductName, stock AS Stock, price AS Price, wholesale_price AS WholesalePrice, unit AS Unit, purchase_unit AS PurchaseUnit, sale_unit AS SaleUnit, conversion_factor AS ConversionFactor, unit_cost AS UnitCost, buying_price AS BuyingPrice, description AS Description, p_status AS PStatus, created_at AS CreatedAt, expiry_date AS ExpiryDate FROM products WHERE pid = @Pid";
        const string sqlMinimal = "SELECT pid AS Pid, cat_id AS CatId, brand_id AS BrandId, product_name AS ProductName, stock AS Stock, price AS Price, buying_price AS BuyingPrice, description AS Description, p_status AS PStatus, created_at AS CreatedAt, expiry_date AS ExpiryDate FROM products WHERE pid = @Pid";
        using var conn = DatabaseFactory.CreateConnection();
        try
        {
            return await conn.QueryFirstOrDefaultAsync<Product>(sqlFull, new { Pid = pid });
        }
        catch (MySqlException ex) when (IsUnknownColumn(ex))
        {
            return await conn.QueryFirstOrDefaultAsync<Product>(sqlMinimal, new { Pid = pid });
        }
    }

    public async Task<int> AddAsync(Product product)
    {
        const string sqlFull = @"
            INSERT INTO products (cat_id, brand_id, supplier_id, product_name, stock, price, wholesale_price, unit,
                purchase_unit, sale_unit, conversion_factor, buying_price, description, expiry_date, p_status)
            VALUES (@CatId, @BrandId, @SupplierId, @ProductName, @Stock, @Price, @WholesalePrice, @Unit,
                @PurchaseUnit, @SaleUnit, @ConversionFactor, @BuyingPrice, @Description, @ExpiryDate, @PStatus);
            SELECT LAST_INSERT_ID();";
        const string sqlMinimal = @"
            INSERT INTO products (cat_id, brand_id, product_name, stock, price, buying_price, description, expiry_date, p_status)
            VALUES (@CatId, @BrandId, @ProductName, @Stock, @Price, @BuyingPrice, @Description, @ExpiryDate, @PStatus);
            SELECT LAST_INSERT_ID();";
        using var conn = DatabaseFactory.CreateConnection();
        try
        {
            return await conn.ExecuteScalarAsync<int>(sqlFull, product);
        }
        catch (MySqlException ex) when (IsUnknownColumn(ex))
        {
            return await conn.ExecuteScalarAsync<int>(sqlMinimal, product);
        }
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        const string sqlFull = @"
            UPDATE products SET cat_id=@CatId, brand_id=@BrandId, supplier_id=@SupplierId, product_name=@ProductName,
                stock=@Stock, price=@Price, wholesale_price=@WholesalePrice, unit=@Unit, purchase_unit=@PurchaseUnit,
                sale_unit=@SaleUnit, conversion_factor=@ConversionFactor, buying_price=@BuyingPrice,
                description=@Description, p_status=@PStatus, expiry_date=@ExpiryDate
            WHERE pid=@Pid";
        const string sqlMinimal = @"
            UPDATE products SET cat_id=@CatId, brand_id=@BrandId, product_name=@ProductName,
                stock=@Stock, price=@Price, buying_price=@BuyingPrice,
                description=@Description, p_status=@PStatus, expiry_date=@ExpiryDate
            WHERE pid=@Pid";
        using var conn = DatabaseFactory.CreateConnection();
        try
        {
            var rows = await conn.ExecuteAsync(sqlFull, product);
            return rows > 0;
        }
        catch (MySqlException ex) when (IsUnknownColumn(ex))
        {
            var rows = await conn.ExecuteAsync(sqlMinimal, product);
            return rows > 0;
        }
    }

    private static bool IsUnknownColumn(MySqlException ex) =>
        ex.Message.Contains("Unknown column", StringComparison.OrdinalIgnoreCase);

    public async Task<(bool Success, string Message)> AddStockAsync(int pid, int quantityToAdd)
    {
        if (quantityToAdd <= 0)
            return (false, "Quantity must be greater than 0.");
        var product = await GetByIdAsync(pid);
        if (product == null)
            return (false, "Product not found.");
        var newStock = product.Stock + quantityToAdd;
        const string sql = "UPDATE products SET stock = @Stock WHERE pid = @Pid";
        using var conn = DatabaseFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { Stock = newStock, Pid = pid });
        return rows > 0
            ? (true, $"Added {quantityToAdd} to stock. New total: {newStock}")
            : (false, "Failed to update stock.");
    }

    public async Task<bool> DeleteAsync(int pid)
    {
        const string sql = "DELETE FROM products WHERE pid = @Pid";
        using var conn = DatabaseFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { Pid = pid });
        return rows > 0;
    }
}
