using System.Collections.Generic;
using System.Threading.Tasks;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public interface IStockReconciliationService
{
    Task<IReadOnlyList<StockReconciliation>> GetAllAsync(int limit = 50);
    Task<IReadOnlyList<StockReconciliation>> GetPendingAsync();
    Task<IReadOnlyList<Product>> GetProductsForReconciliationAsync();
    Task<(bool Success, string Message)> CreateAsync(int productId, int physicalCount, string? notes);
    Task<(bool Success, string Message)> ApproveAsync(int id);
    Task<(bool Success, string Message)> RejectAsync(int id);
}
