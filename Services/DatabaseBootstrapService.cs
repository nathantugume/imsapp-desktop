using System.Diagnostics;
using System.Reflection;
using Dapper;
using imsapp_desktop.Config;
using imsapp_desktop.Data;
using MySqlConnector;

namespace imsapp_desktop.Services;

/// <summary>
/// Ensures database connectivity: uses existing MySQL if available, otherwise starts bundled MySQL.
/// </summary>
public static class DatabaseBootstrapService
{
    public enum BootstrapResult { UsingExisting, StartedBundled, Failed }

    /// <summary>
    /// Ensures we can connect to MySQL. Tries existing first, then starts bundled if configured.
    /// Updates AppSettings.ConnectionString when using bundled.
    /// </summary>
    public static async Task<(BootstrapResult Result, string Message)> EnsureDatabaseAsync()
    {
        AppConfig.Load();
        AppSettings.ConnectionString = AppConfig.ConnectionString;

        // 1. Try to connect with current config
        if (await TryConnectAsync(AppSettings.ConnectionString))
        {
            await CreateDatabaseIfNeededAsync();
            await RunSchemaIfNeededAsync();
            return (BootstrapResult.UsingExisting, "Connected to existing MySQL.");
        }

        // 2. Check if bundled MySQL is configured and available
        var bundledPath = ResolveBundledMySQLPath();
        if (string.IsNullOrEmpty(bundledPath))
            return (BootstrapResult.Failed, "Cannot connect to MySQL. Please configure the connection in Settings or install with bundled database.");

        var mysqld = Path.Combine(bundledPath, "bin", "mysqld.exe");
        if (!File.Exists(mysqld))
            return (BootstrapResult.Failed, $"Bundled MySQL not found at {mysqld}");

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "imsapp-desktop", "mysql-data");

        // 3. Initialize data directory if needed
        if (!Directory.Exists(dataDir) || !Directory.EnumerateFileSystemEntries(dataDir).Any())
        {
            var initResult = InitializeDataDirectory(mysqld, dataDir);
            if (!initResult.Success)
                return (BootstrapResult.Failed, initResult.Message);
        }

        // 4. Start mysqld if not already running
        if (!await IsBundledMySQLRunningAsync())
        {
            var startResult = StartBundledMySQL(mysqld, dataDir);
            if (!startResult.Success)
                return (BootstrapResult.Failed, startResult.Message);

            // Wait for MySQL to accept connections (connect without database - imsapp may not exist yet)
            await Task.Delay(3000);
            for (int i = 0; i < 10; i++)
            {
                if (await TryConnectAsync(AppConfig.GetBundledConnectionStringNoDb()))
                    break;
                await Task.Delay(1000);
            }
        }

        // 5. Switch to bundled connection and create database
        AppSettings.ConnectionString = AppConfig.GetBundledConnectionString();
        if (!await TryConnectAsync(AppSettings.ConnectionString))
            return (BootstrapResult.Failed, "Bundled MySQL started but connection failed. Please check the logs.");

        await CreateDatabaseIfNeededAsync();
        await RunSchemaIfNeededAsync();
        AppConfig.ConnectionString = AppSettings.ConnectionString;
        AppConfig.Save();

        return (BootstrapResult.StartedBundled, "Bundled MySQL started successfully.");
    }

    private static async Task<bool> TryConnectAsync(string connectionString)
    {
        try
        {
            await using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? ResolveBundledMySQLPath()
    {
        if (!string.IsNullOrWhiteSpace(AppConfig.BundledMySQLPath) && Directory.Exists(AppConfig.BundledMySQLPath))
            return AppConfig.BundledMySQLPath;

        // Check next to executable (installer places mysql folder alongside app)
        var appDir = AppContext.BaseDirectory;
        var pathNextToExe = Path.Combine(appDir, "mysql");
        if (Directory.Exists(pathNextToExe) && File.Exists(Path.Combine(pathNextToExe, "bin", "mysqld.exe")))
            return pathNextToExe;

        return null;
    }

    private static (bool Success, string Message) InitializeDataDirectory(string mysqld, string dataDir)
    {
        try
        {
            Directory.CreateDirectory(dataDir);
            var psi = new ProcessStartInfo
            {
                FileName = mysqld,
                Arguments = $"--initialize-insecure --datadir=\"{dataDir}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            var err = p?.StandardError.ReadToEnd() ?? "";
            p?.WaitForExit(60000);
            if (p?.ExitCode != 0)
                return (false, $"MySQL init failed: {err}");
            return (true, "");
        }
        catch (Exception ex)
        {
            return (false, $"MySQL init failed: {ex.Message}");
        }
    }

    private static async Task<bool> IsBundledMySQLRunningAsync()
    {
        return await TryConnectAsync(AppConfig.GetBundledConnectionStringNoDb());
    }

    private static (bool Success, string Message) StartBundledMySQL(string mysqld, string dataDir)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = mysqld,
                Arguments = $"--datadir=\"{dataDir}\" --port={AppConfig.BundledPort} --standalone",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
            return (true, "");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to start MySQL: {ex.Message}");
        }
    }

    private static async Task CreateDatabaseIfNeededAsync()
    {
        try
        {
            var connStrNoDb = new MySqlConnectionStringBuilder(AppSettings.ConnectionString)
            {
                Database = ""
            };
            await using var conn = new MySqlConnection(connStrNoDb.ConnectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync("CREATE DATABASE IF NOT EXISTS imsapp");
        }
        catch { /* ignore */ }
    }

    private static async Task RunSchemaIfNeededAsync()
    {
        try
        {
            await using var conn = new MySqlConnection(AppSettings.ConnectionString);
            await conn.OpenAsync();
            var exists = await conn.ExecuteScalarAsync<int?>(
                "SELECT 1 FROM information_schema.tables WHERE table_schema = 'imsapp' AND table_name = 'users' LIMIT 1");
            if (exists == 1)
                return;

            var schema = LoadEmbeddedSchema();
            if (string.IsNullOrWhiteSpace(schema))
                return;

            foreach (var stmt in SplitStatements(schema))
            {
                if (string.IsNullOrWhiteSpace(stmt))
                    continue;
                try
                {
                    await conn.ExecuteAsync(stmt.TrimEnd(';'));
                }
                catch { /* ignore individual statement errors */ }
            }
        }
        catch { /* ignore */ }
    }

    private static string LoadEmbeddedSchema()
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("Schema.sql", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(resourceName))
            return "";
        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null)
            return "";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static IEnumerable<string> SplitStatements(string sql)
    {
        var statements = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (var line in sql.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("--") || string.IsNullOrWhiteSpace(trimmed))
                continue;
            current.AppendLine(line);
            if (trimmed.EndsWith(";"))
            {
                var stmt = current.ToString().Trim();
                if (!string.IsNullOrEmpty(stmt))
                    statements.Add(stmt);
                current.Clear();
            }
        }
        if (current.Length > 0)
        {
            var stmt = current.ToString().Trim();
            if (!string.IsNullOrEmpty(stmt))
                statements.Add(stmt);
        }
        return statements;
    }

    /// <summary>
    /// Stops the bundled MySQL process (optional, call on app exit).
    /// Bundled MySQL typically keeps running for faster next launch.
    /// </summary>
    public static void StopBundledMySQLIfRunning()
    {
        // Optional: could kill mysqld processes started by us. Skipped to keep MySQL running for next launch.
    }
}
