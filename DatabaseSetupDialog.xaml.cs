using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MySqlConnector;
using imsapp_desktop.Config;

namespace imsapp_desktop;

public sealed partial class DatabaseSetupDialog : ContentDialog
{
    private readonly ObservableCollection<DetectedInstance> _detected = [];

    public DatabaseSetupDialog()
    {
        InitializeComponent();
        LoadCurrent();
        DetectedList.ItemsSource = _detected;
    }

    private void LoadCurrent()
    {
        try
        {
            AppConfig.Load();
            var builder = new MySqlConnectionStringBuilder(AppConfig.ConnectionString);
            ServerBox.Text = builder.Server;
            PortBox.Value = builder.Port;
            DatabaseBox.Text = builder.Database;
            UserBox.Text = builder.UserID;
            PasswordBox.Password = builder.Password ?? "";
        }
        catch
        {
            ServerBox.Text = "localhost";
            PortBox.Value = 3306;
            DatabaseBox.Text = "imsapp";
            UserBox.Text = "root";
            PasswordBox.Password = "";
        }
    }

    private async void Save_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        var server = ServerBox.Text?.Trim() ?? "localhost";
        var database = DatabaseBox.Text?.Trim() ?? "imsapp";
        var user = UserBox.Text?.Trim() ?? "root";

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(user))
        {
            args.Cancel = true;
            var dlg = new ContentDialog { Title = "Error", Content = "Server, Database, and User are required.", CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
            deferral.Complete();
            return;
        }

        try
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = server,
                Port = (uint)PortBox.Value,
                Database = database,
                UserID = user,
                Password = PasswordBox.Password ?? ""
            };
            AppConfig.ConnectionString = builder.ConnectionString;
            AppConfig.Save();
        }
        catch (Exception ex)
        {
            args.Cancel = true;
            var dlg = new ContentDialog { Title = "Error", Content = ex.Message, CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
            deferral.Complete();
            return;
        }

        deferral.Complete();

        // Restart the application
        try
        {
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
            }
        }
        catch { /* ignore */ }

        Microsoft.UI.Xaml.Application.Current.Exit();
    }

    private async void Detect_Click(object sender, RoutedEventArgs e)
    {
        DetectButton.IsEnabled = false;
        DetectStatus.Text = "Scanning...";
        _detected.Clear();

        var server = ServerBox.Text?.Trim() ?? "127.0.0.1";
        var user = UserBox.Text?.Trim() ?? "root";
        var password = PasswordBox.Password ?? "";
        var ports = new[] { 3306, 3307, 3308, 33060 };

        var found = await Task.Run(async () =>
        {
            var list = new List<DetectedInstance>();
            foreach (var port in ports)
            {
                try
                {
                    var connStr = $"Server={server};Port={port};User={user};Password={password};ConnectionTimeout=2;ConnectionReset=true";
                    await using var conn = new MySqlConnection(connStr);
                    await conn.OpenAsync();

                    var databases = new List<string>();
                    await using (var cmd = new MySqlCommand("SHOW DATABASES", conn))
                    await using (var r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                            databases.Add(r.GetString(0));
                    }
                    list.Add(new DetectedInstance(server, (uint)port, databases));
                }
                catch { /* skip */ }
            }
            return list;
        });

        foreach (var inst in found)
            _detected.Add(inst);

        DetectButton.IsEnabled = true;
        DetectStatus.Text = _detected.Count > 0 ? $"Found {_detected.Count} instance(s)" : "No instances found";
    }

    private void DetectedList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is DetectedInstance inst)
        {
            ServerBox.Text = inst.Server;
            PortBox.Value = inst.Port;
            if (inst.Databases.Contains("imsapp"))
                DatabaseBox.Text = "imsapp";
            else if (inst.Databases.Count > 0)
                DatabaseBox.Text = inst.Databases.FirstOrDefault(d => !d.StartsWith("information_schema") && d != "mysql" && d != "performance_schema") ?? inst.Databases[0];
        }
    }

    public sealed record DetectedInstance(string Server, uint Port, List<string> Databases)
    {
        public string Display => $"{Server}:{Port} — {string.Join(", ", Databases.Take(6))}{(Databases.Count > 6 ? "..." : "")}";
    }
}
