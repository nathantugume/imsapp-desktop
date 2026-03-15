using System;
using System.IO;

namespace imsapp_desktop.Services;

/// <summary>
/// Writes crash and startup logs to %LocalAppData%\imsapp-desktop\ for debugging.
/// </summary>
public static class CrashLog
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "imsapp-desktop", "startup.log");

    public static void Write(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
            File.AppendAllText(LogPath, line);
        }
        catch { /* ignore */ }
    }

    public static void WriteException(string context, Exception ex)
    {
        Write($"{context}: {ex.GetType().Name}: {ex.Message}");
        Write($"  Stack: {ex.StackTrace}");
        if (ex.InnerException != null)
            Write($"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
    }
}
