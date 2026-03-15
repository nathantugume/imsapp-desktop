using System;
using System.IO;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;
using imsapp_desktop.Services;

namespace imsapp_desktop;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        // Write immediately with minimal code - before any WinRT/COM init that might crash
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "imsapp-desktop");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "startup.log"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Main() entered{Environment.NewLine}");
        }
        catch { /* ignore */ }

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            CrashLog.WriteException("UnhandledException", (Exception)e.ExceptionObject);
        };
        CrashLog.Write($"Starting from: {AppContext.BaseDirectory}");
        try
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            CrashLog.Write("ComWrappers initialized");
            Microsoft.UI.Xaml.Application.Start(p =>
            {
                try
                {
                    CrashLog.Write("Application.Start callback entered");
                    var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    new App();
                    CrashLog.Write("App created");
                }
                catch (Exception ex)
                {
                    CrashLog.WriteException("Application.Start callback", ex);
                    throw;
                }
            });
            CrashLog.Write("Application exited normally");
            return 0;
        }
        catch (Exception ex)
        {
            CrashLog.WriteException("FATAL", ex);
            return 1;
        }
    }
}
