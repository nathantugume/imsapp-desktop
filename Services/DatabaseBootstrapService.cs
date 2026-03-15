using System.Diagnostics;
using System.Reflection;
using Dapper;
using imsapp_desktop.Config;
using imsapp_desktop.Data;
using MySqlConnector;

namespace imsapp_desktop.Services;

/// <summary>
/// Progress reported during database bootstrap for UI feedback.
/// </summary>
public record DatabaseBootstrapProgress(string Status, int ProgressPercent, bool IsIndeterminate);

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
    /// <param name="progress">Optional progress reporter for UI (status text, progress bar).</param>
    public static async Task<(BootstrapResult Result, string Message)> EnsureDatabaseAsync(
        IProgress<DatabaseBootstrapProgress>? progress = null)
    {
        void Report(string status, int percent, bool indeterminate = false) =>
            progress?.Report(new DatabaseBootstrapProgress(status, percent, indeterminate));

        AppConfig.Load();
        AppSettings.ConnectionString = AppConfig.ConnectionString;

        // 1. Check if database is already running (existing MySQL or bundled)
        Report("Checking database connection...", 10, true);
        if (await TryConnectAsync(AppSettings.ConnectionString))
        {
            Report("Database ready.", 100);
            await CreateDatabaseIfNeededAsync();
            await RunSchemaIfNeededAsync();
            return (BootstrapResult.UsingExisting, "Connected to existing MySQL.");
        }

        // 1b. Try connecting without database (server may be up but imsapp not created yet)
        var connNoDb = new MySqlConnectionStringBuilder(AppSettings.ConnectionString) { Database = "" };
        if (await TryConnectAsync(connNoDb.ConnectionString))
        {
            Report("Setting up database...", 80);
            await CreateDatabaseIfNeededAsync();
            await RunSchemaIfNeededAsync();
            if (await TryConnectAsync(AppSettings.ConnectionString))
            {
                Report("Database ready.", 100);
                return (BootstrapResult.UsingExisting, "Connected to existing MySQL.");
            }
        }

        // 2. Check if bundled MySQL is configured and available
        Report("Database not found. Checking for bundled database...", 15);
        var bundledPath = ResolveBundledMySQLPath();
        if (string.IsNullOrEmpty(bundledPath))
            return (BootstrapResult.Failed, "Cannot connect to MySQL. If you have MySQL installed, edit %LocalAppData%\\imsapp-desktop\\appsettings.json and set ConnectionString (e.g. Server=localhost;Database=imsapp;User=root;Password=;). Or use the installer that includes the database.");

        var serverExe = ResolveServerExe(bundledPath);
        if (string.IsNullOrEmpty(serverExe))
            return (BootstrapResult.Failed, "Bundled MySQL/MariaDB not found (need bin\\mysqld.exe or bin\\mariadbd.exe)");

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "imsapp-desktop", "mysql-data");

        // 3. Initialize data directory if needed (first run)
        if (!Directory.Exists(dataDir) || !Directory.EnumerateFileSystemEntries(dataDir).Any())
        {
            Report("Initializing database (first run)...", 25, true);
            var initResult = InitializeDataDirectory(serverExe, dataDir);
            if (!initResult.Success)
                return (BootstrapResult.Failed, initResult.Message);
        }

        // 4. Check if bundled MySQL is already running, or start it
        if (await IsBundledMySQLRunningAsync())
        {
            Report("Database already running. Setting up...", 60);
        }
        else
        {
            Report("Starting database...", 35, true);
            var startResult = StartBundledMySQL(serverExe, dataDir);
            if (!startResult.Success)
                return (BootstrapResult.Failed, startResult.Message);

            Report("Waiting for database to start...", 50, true);
            await Task.Delay(2000);
            for (int i = 0; i < 15; i++)
            {
                if (await TryConnectAsync(AppConfig.GetBundledConnectionStringNoDb()))
                    break;
                Report($"Waiting for database to start... (attempt {i + 1}/15)", 50 + (i * 2), true);
                await Task.Delay(1000);
            }
        }

        // 5. Switch to bundled connection, create database, then verify
        AppSettings.ConnectionString = AppConfig.GetBundledConnectionString();
        if (!await TryConnectAsync(AppConfig.GetBundledConnectionStringNoDb()))
            return (BootstrapResult.Failed, "Bundled MySQL failed to start. Check the error log in %LocalAppData%\\imsapp-desktop\\mysql-data\\ or configure an external MySQL in appsettings.json.");

        Report("Setting up database...", 85);
        await CreateDatabaseIfNeededAsync();
        await RunSchemaIfNeededAsync();
        if (!await TryConnectAsync(AppSettings.ConnectionString))
            return (BootstrapResult.Failed, "Could not connect to imsapp database after creating it.");

        AppConfig.ConnectionString = AppSettings.ConnectionString;
        AppConfig.Save();
        Report("Database ready.", 100);

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
        if (Directory.Exists(pathNextToExe) && ResolveServerExe(pathNextToExe) != null)
            return pathNextToExe;

        return null;
    }

    private static string? ResolveServerExe(string bundledPath)
    {
        var mysqld = Path.Combine(bundledPath, "bin", "mysqld.exe");
        if (File.Exists(mysqld)) return mysqld;
        var mariadbd = Path.Combine(bundledPath, "bin", "mariadbd.exe");
        if (File.Exists(mariadbd)) return mariadbd;
        return null;
    }

    private static (bool Success, string Message) InitializeDataDirectory(string serverExe, string dataDir)
    {
        try
        {
            Directory.CreateDirectory(dataDir);
            var mysqlBase = Path.GetDirectoryName(Path.GetDirectoryName(serverExe)) ?? ".";
            var psi = new ProcessStartInfo
            {
                FileName = serverExe,
                Arguments = $"--initialize-insecure --datadir=\"{dataDir}\"",
                WorkingDirectory = mysqlBase,
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

    private static (bool Success, string Message) StartBundledMySQL(string serverExe, string dataDir)
    {
        try
        {
            var mysqlBase = Path.GetDirectoryName(Path.GetDirectoryName(serverExe)) ?? ".";
            var psi = new ProcessStartInfo
            {
                FileName = serverExe,
                Arguments = $"--datadir=\"{dataDir}\" --port={AppConfig.BundledPort} --standalone",
                WorkingDirectory = mysqlBase,
                UseShellExecute = false,
                CreateNoWindow = true
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
