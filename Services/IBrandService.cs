using System.Collections.Generic;
using System.Threading.Tasks;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public interface IBrandService
{
    Task<IReadOnlyList<Brand>> GetAllAsync();
    Task<Brand?> GetByIdAsync(int brandId);
    Task<int> AddAsync(Brand brand);
    Task<bool> UpdateAsync(Brand brand);
    Task<bool> DeleteAsync(int brandId);
}
