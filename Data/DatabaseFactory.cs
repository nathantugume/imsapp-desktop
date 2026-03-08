using System.Data;
using imsapp_desktop.Config;
using MySqlConnector;

namespace imsapp_desktop.Data;

/// <summary>
/// Creates MySQL connections. Uses AppSettings which may be updated by DatabaseBootstrapService.
/// </summary>
public static class DatabaseFactory
{
    public static IDbConnection CreateConnection()
    {
        return new MySqlConnection(AppSettings.ConnectionString);
    }
}
