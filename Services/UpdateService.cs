using Velopack;
using Velopack.Sources;

namespace imsapp_desktop.Services;

/// <summary>
/// Wraps Velopack for checking and applying app updates.
/// </summary>
public interface IUpdateService
{
    Task<Velopack.UpdateInfo?> CheckForUpdatesAsync();
    Task DownloadUpdatesAsync(Velopack.UpdateInfo update);
    void ApplyUpdatesAndRestart(Velopack.UpdateInfo update);
    bool IsUpdateSupported { get; }
}

public class UpdateService : IUpdateService
{
    private UpdateManager? _manager;

    public bool IsUpdateSupported =>
        !string.IsNullOrWhiteSpace(Config.AppSettings.UpdateUrl);

    private UpdateManager? GetManager()
    {
        var url = Config.AppSettings.UpdateUrl?.Trim();
        if (string.IsNullOrWhiteSpace(url)) return null;

        if (_manager != null) return _manager;

        if (url.Contains("github.com", StringComparison.OrdinalIgnoreCase))
        {
            var source = new GithubSource(url, accessToken: "", prerelease: false);
            _manager = new UpdateManager(source);
        }
        else
        {
            _manager = new UpdateManager(url);
        }
        return _manager;
    }

    public async Task<Velopack.UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            var mgr = GetManager();
            if (mgr == null) return null;
            return await mgr.CheckForUpdatesAsync();
        }
        catch
        {
            return null;
        }
    }

    public async Task DownloadUpdatesAsync(Velopack.UpdateInfo update)
    {
        var mgr = GetManager();
        if (mgr == null) return;
        await mgr.DownloadUpdatesAsync(update);
    }

    public void ApplyUpdatesAndRestart(Velopack.UpdateInfo update)
    {
        var mgr = GetManager();
        if (mgr == null) return;
        mgr.ApplyUpdatesAndRestart(update);
    }
}
