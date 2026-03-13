namespace imsapp_desktop.Config;

/// <summary>
/// Application settings. Connection string - same MySQL database as PHP app.
/// Default: no password (WAMP). If your MySQL uses a password, change it here.
/// </summary>
public static class AppSettings
{
    public const string DefaultConnectionString = "Server=localhost;Database=imsapp;User=root;Password=;";
    
    /// <summary>
    /// Connection string for MySQL. Override via user config file if needed.
    /// </summary>
    public static string ConnectionString { get; set; } = DefaultConnectionString;
}
