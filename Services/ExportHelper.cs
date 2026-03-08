using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace imsapp_desktop.Services;

/// <summary>
/// Reusable UI helpers for export: FileSavePicker, success/error dialogs.
/// </summary>
public static class ExportHelper
{
    public static async Task<StorageFile?> PickSaveFileAsync(XamlRoot xamlRoot, string choiceLabel, string extension, string suggestedFileName)
    {
        var window = App.MainWindowInstance;
        if (window == null)
        {
            await ShowErrorAsync(xamlRoot, "Could not open save dialog.");
            return null;
        }

        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName,
            FileTypeChoices = { { choiceLabel, new[] { extension } } }
        };
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
        return await picker.PickSaveFileAsync();
    }

    public static async Task ShowSuccessAsync(XamlRoot xamlRoot, int rowCount, string fileName)
    {
        var dlg = new ContentDialog { Title = "Export", Content = $"Exported {rowCount} rows to {fileName}", CloseButtonText = "OK" };
        dlg.XamlRoot = xamlRoot;
        await dlg.ShowAsync();
    }

    public static async Task ShowErrorAsync(XamlRoot xamlRoot, string message)
    {
        var dlg = new ContentDialog { Title = "Export", Content = message, CloseButtonText = "OK" };
        dlg.XamlRoot = xamlRoot;
        await dlg.ShowAsync();
    }

    public static string DefaultFileName(string baseName) => $"{baseName}_{DateTime.Now:yyyy-MM-dd}";

    /// <summary>Export table data to CSV/Excel/PDF. Returns true if exported.</summary>
    public static async Task<bool> ExportTableAsync<T>(XamlRoot xamlRoot, string title, string baseFileName,
        IReadOnlyList<T> rows, IReadOnlyList<TableExportService.ExportColumn<T>> columns,
        string format)
    {
        if (rows.Count == 0)
        {
            await ShowErrorAsync(xamlRoot, "No data to export.");
            return false;
        }

        var (ext, label) = format switch
        {
            "csv" => (".csv", "CSV file"),
            "excel" => (".xlsx", "Excel workbook"),
            "pdf" => (".pdf", "PDF document"),
            _ => (".csv", "CSV file")
        };

        var file = await PickSaveFileAsync(xamlRoot, label, ext, DefaultFileName(baseFileName));
        if (file == null) return false;

        var path = file.Path;
        if (string.IsNullOrEmpty(path) && format != "csv")
        {
            await ShowErrorAsync(xamlRoot, "Could not get file path for save location.");
            return false;
        }

        try
        {
            switch (format)
            {
                case "csv":
                    var csv = TableExportService.ExportToCsv(rows, columns);
                    await FileIO.WriteTextAsync(file, csv);
                    break;
                case "excel":
                    TableExportService.ExportToExcel(rows, columns, title, null, path);
                    break;
                case "pdf":
                    await Task.Run(() => TableExportService.ExportToPdf(rows, columns, title, null, path));
                    break;
            }
            await ShowSuccessAsync(xamlRoot, rows.Count, file.Name);
            return true;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(xamlRoot, "Export failed: " + ex.Message);
            return false;
        }
    }
}
