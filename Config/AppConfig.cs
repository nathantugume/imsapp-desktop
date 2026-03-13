using System.IO;
using System.Text.Json;

namespace imsapp_desktop.Config;

/// <summary>
/// Loads and saves app configuration (connection string, bundled MySQL path).
/// Config file: %LocalAppData%\imsapp-desktop\appsettings.json
/// </summary>
public static class AppConfig
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "imsapp-desktop");
    private static readonly string ConfigPath = Path.Combine(ConfigDir, "appsettings.json");

    public static string ConnectionString { get; set; } = AppSettings.DefaultConnectionString;
    public static string? BundledMySQLPath { get; set; }
    public static int BundledPort { get; set; } = 3307;

    public static void Load()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return;

            var json = File.ReadAllText(ConfigPath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("ConnectionString", out var cs))
                ConnectionString = cs.GetString() ?? AppSettings.DefaultConnectionString;
            if (root.TryGetProperty("BundledMySQLPath", out var path))
                BundledMySQLPath = string.IsNullOrWhiteSpace(path.GetString()) ? null : path.GetString();
            if (root.TryGetProperty("BundledPort", out var port))
                BundledPort = port.TryGetInt32(out var p) ? p : 3307;
        }
        catch { /* use defaults */ }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var obj = new Dictionary<string, object?>
            {
                ["ConnectionString"] = ConnectionString,
                ["BundledMySQLPath"] = BundledMySQLPath,
                ["BundledPort"] = BundledPort
            };
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch { /* ignore */ }
    }

    public static string GetBundledConnectionString()
    {
        var builder = new MySqlConnector.MySqlConnectionStringBuilder(ConnectionString)
        {
            Server = "127.0.0.1",
            Port = (uint)BundledPort,
            Database = "imsapp",
            UserID = "root",
            Password = ""
        };
        return builder.ConnectionString;
    }

    /// <summary>Connection string for bundled MySQL without database (for initial connection check).</summary>
    public static string GetBundledConnectionStringNoDb()
    {
        var builder = new MySqlConnector.MySqlConnectionStringBuilder(ConnectionString)
        {
            Server = "127.0.0.1",
            Port = (uint)BundledPort,
            Database = "",
            UserID = "root",
            Password = ""
        };
        return builder.ConnectionString;
    }
}
