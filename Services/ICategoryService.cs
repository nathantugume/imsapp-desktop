using System.Collections.Generic;
using System.Threading.Tasks;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int catId);
    Task<int> AddAsync(Category category);
    Task<bool> UpdateAsync(Category category);
    Task<bool> DeleteAsync(int catId);
}
