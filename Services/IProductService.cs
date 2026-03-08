using System.Collections.Generic;
using System.Threading.Tasks;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int pid);
    Task<int> AddAsync(Product product);
    Task<bool> UpdateAsync(Product product);
    Task<bool> DeleteAsync(int pid);
    Task<(bool Success, string Message)> AddStockAsync(int pid, int quantityToAdd);
}
