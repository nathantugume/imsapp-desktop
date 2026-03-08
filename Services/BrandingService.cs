using System.IO;
using System.Text.Json;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public class BrandingService : IBrandingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string GetSettingsPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(localAppData, "imsapp-desktop");
        Directory.CreateDirectory(appFolder);
        return Path.Combine(appFolder, "branding.json");
    }

    public BrandingSettings Current { get; private set; } = new();

    public async Task LoadAsync()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
            return;

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var loaded = JsonSerializer.Deserialize<BrandingSettings>(json, JsonOptions);
            if (loaded != null)
                Current = loaded;
        }
        catch
        {
            // Keep defaults on load error
        }
    }

    public async Task SaveAsync(BrandingSettings settings)
    {
        Current = settings;
        var path = GetSettingsPath();
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }
}
