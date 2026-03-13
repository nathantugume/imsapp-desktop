using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public interface IBrandingService
{
    BrandingSettings Current { get; }
    Task LoadAsync();
    Task SaveAsync(BrandingSettings settings);
}
