using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using imsapp_desktop.Data;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop.Services;

internal class ReconciliationRow { public int ProductId { get; set; } public int PhysicalCount { get; set; } }

public class StockReconciliationService : IStockReconciliationService
{
    private readonly IAuthService _auth = ServiceLocator.Auth;

    public async Task<IReadOnlyList<StockReconciliation>> GetAllAsync(int limit = 50)
    {
        const string sql = @"
            SELECT sr.id AS Id, sr.product_id AS ProductId, sr.system_stock AS SystemStock,
                sr.physical_count AS PhysicalCount, sr.difference AS Difference,
                sr.reconciliation_date AS ReconciliationDate, sr.status AS Status, sr.notes AS Notes,
                sr.created_by AS CreatedBy, sr.approved_by AS ApprovedBy, sr.approved_at AS ApprovedAt,
                p.product_name AS ProductName, u.name AS CreatedByName, approver.name AS ApprovedByName
            FROM stock_reconciliations sr
            JOIN products p ON sr.product_id = p.pid
            LEFT JOIN users u ON sr.created_by = u.id
            LEFT JOIN users approver ON sr.approved_by = approver.id
            ORDER BY sr.reconciliation_date DESC
            LIMIT @Limit";
        using var conn = DatabaseFactory.CreateConnection();
        var list = await conn.QueryAsync<StockReconciliation>(sql, new { Limit = limit });
        return list.ToList();
    }

    public async Task<IReadOnlyList<StockReconciliation>> GetPendingAsync()
    {
        const string sql = @"
            SELECT sr.id AS Id, sr.product_id AS ProductId, sr.system_stock AS SystemStock,
                sr.physical_count AS PhysicalCount, sr.difference AS Difference,
                sr.reconciliation_date AS ReconciliationDate, sr.status AS Status, sr.notes AS Notes,
                sr.created_by AS CreatedBy, sr.approved_by AS ApprovedBy, sr.approved_at AS ApprovedAt,
                p.product_name AS ProductName, u.name AS CreatedByName, approver.name AS ApprovedByName
            FROM stock_reconciliations sr
            JOIN products p ON sr.product_id = p.pid
            LEFT JOIN users u ON sr.created_by = u.id
            LEFT JOIN users approver ON sr.approved_by = approver.id
            WHERE sr.status = 'pending'
            ORDER BY sr.reconciliation_date DESC";
        using var conn = DatabaseFactory.CreateConnection();
        var list = await conn.QueryAsync<StockReconciliation>(sql);
        return list.ToList();
    }

    public async Task<IReadOnlyList<Product>> GetProductsForReconciliationAsync()
    {
        const string sql = "SELECT pid AS Pid, product_name AS ProductName, stock AS Stock FROM products WHERE p_status = '1' ORDER BY product_name";
        using var conn = DatabaseFactory.CreateConnection();
        var list = await conn.QueryAsync<Product>(sql);
        return list.ToList();
    }

    public async Task<(bool Success, string Message)> CreateAsync(int productId, int physicalCount, string? notes)
    {
        var userId = _auth.CurrentUser?.Id ?? 0;
        if (userId == 0)
            return (false, "Not logged in.");

        const string getStockSql = "SELECT stock FROM products WHERE pid = @Pid";
        using var conn = DatabaseFactory.CreateConnection();
        var systemStock = await conn.ExecuteScalarAsync<int?>(getStockSql, new { Pid = productId });
        if (systemStock == null)
            return (false, "Product not found.");

        var difference = physicalCount - systemStock.Value;

        const string insertSql = @"INSERT INTO stock_reconciliations (product_id, system_stock, physical_count, difference, created_by, notes)
            VALUES (@ProductId, @SystemStock, @PhysicalCount, @Difference, @CreatedBy, @Notes)";
        await conn.ExecuteAsync(insertSql, new
        {
            ProductId = productId,
            SystemStock = systemStock.Value,
            PhysicalCount = physicalCount,
            Difference = difference,
            CreatedBy = userId,
            Notes = notes ?? ""
        });
        return (true, "Reconciliation created.");
    }

    public async Task<(bool Success, string Message)> ApproveAsync(int id)
    {
        var userId = _auth.CurrentUser?.Id ?? 0;
        if (userId == 0)
            return (false, "Not logged in.");

        const string getSql = "SELECT product_id AS ProductId, physical_count AS PhysicalCount FROM stock_reconciliations WHERE id = @Id AND status = 'pending'";
        using var conn = DatabaseFactory.CreateConnection();
        var rec = await conn.QueryFirstOrDefaultAsync<ReconciliationRow>(getSql, new { Id = id });
        if (rec == null)
            return (false, "Reconciliation not found or already processed.");

        const string updateRecSql = "UPDATE stock_reconciliations SET status = 'approved', approved_by = @ApprovedBy, approved_at = NOW() WHERE id = @Id";
        await conn.ExecuteAsync(updateRecSql, new { Id = id, ApprovedBy = userId });

        const string updateProductSql = "UPDATE products SET stock = @Stock WHERE pid = @Pid";
        await conn.ExecuteAsync(updateProductSql, new { Stock = rec.PhysicalCount, Pid = rec.ProductId });

        return (true, "Reconciliation approved.");
    }

    public async Task<(bool Success, string Message)> RejectAsync(int id)
    {
        var userId = _auth.CurrentUser?.Id ?? 0;
        if (userId == 0)
            return (false, "Not logged in.");

        const string sql = "UPDATE stock_reconciliations SET status = 'rejected', approved_by = @ApprovedBy, approved_at = NOW() WHERE id = @Id AND status = 'pending'";
        using var conn = DatabaseFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { Id = id, ApprovedBy = userId });
        return rows > 0 ? (true, "Reconciliation rejected.") : (false, "Reconciliation not found or already processed.");
    }
}
