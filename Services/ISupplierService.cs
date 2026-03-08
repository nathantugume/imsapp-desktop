using System.Collections.Generic;
using System.Threading.Tasks;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public interface ISupplierService
{
    Task<IReadOnlyList<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(int supplierId);
    Task<int> AddAsync(Supplier supplier);
    Task<bool> UpdateAsync(Supplier supplier);
    Task<bool> DeleteAsync(int supplierId);
}
