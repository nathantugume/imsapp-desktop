using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace imsapp_desktop.Services;

/// <summary>
/// Generic table export to CSV, Excel, and PDF. Reusable across Products, Suppliers, Orders, etc.
/// </summary>
public static class TableExportService
{
    /// <summary>Column definition: Header text and value getter for each row.</summary>
    public record ExportColumn<T>(string Header, System.Func<T, string?> GetValue);

    public static string ExportToCsv<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", columns.Select(c => EscapeCsv(c.Header))));
        foreach (var row in rows)
        {
            var values = columns.Select(c => EscapeCsv(c.GetValue(row) ?? ""));
            sb.AppendLine(string.Join(",", values));
        }
        return sb.ToString();
    }

    public static void ExportToExcel<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns, string title, string? subtitle, string filePath)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Export");
        int dataRow = 1;
        if (!string.IsNullOrEmpty(title))
        {
            ws.Cell(dataRow, 1).Value = title;
            ws.Range(dataRow, 1, dataRow, columns.Count).Style.Font.Bold = true;
            dataRow++;
        }
        if (!string.IsNullOrEmpty(subtitle))
        {
            ws.Cell(dataRow, 1).Value = subtitle;
            dataRow++;
        }
        if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(subtitle))
            dataRow++;

        for (int i = 0; i < columns.Count; i++)
            ws.Cell(dataRow, i + 1).Value = columns[i].Header;
        ws.Range(dataRow, 1, dataRow, columns.Count).Style.Font.Bold = true;
        ws.Range(dataRow, 1, dataRow, columns.Count).Style.Fill.BackgroundColor = XLColor.LightGray;
        dataRow++;

        foreach (var row in rows)
        {
            for (int i = 0; i < columns.Count; i++)
                ws.Cell(dataRow, i + 1).Value = columns[i].GetValue(row) ?? "";
            dataRow++;
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public static void ExportToPdf<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns, string title, string? subtitle, string filePath)
    {
        var rowsList = rows.ToList();
        var doc = new PdfDocument();
        doc.Info.Title = title;

        var fontTitle = new XFont("Arial", 14, XFontStyleEx.Bold);
        var fontNormal = new XFont("Arial", 9);
        var fontBold = new XFont("Arial", 9, XFontStyleEx.Bold);
        const double margin = 30;
        const double rowH = 20;

        var colCount = columns.Count;
        var colWidth = 80.0;
        if (colCount > 0)
        {
            var totalPageWidth = 842; // A4 landscape width approx
            colWidth = Math.Max(50, (totalPageWidth - margin * 2) / colCount);
        }
        var cols = Enumerable.Repeat(colWidth, colCount).ToArray();
        var totalW = cols.Sum(c => c);

        PdfPage? page = null;
        XGraphics? gfx = null;
        double y = 0;
        double pageBottom = 0;

        void EnsurePage()
        {
            if (page != null && y + rowH <= pageBottom) return;
            gfx?.Dispose();
            page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            page.Orientation = PdfSharp.PageOrientation.Landscape;
            gfx = XGraphics.FromPdfPage(page);
            pageBottom = page.Height.Point - margin;
            y = margin;

            var b = ServiceLocator.Branding.Current;
            gfx.DrawString(b.BusinessName, fontTitle, XBrushes.Black, new XRect(0, y, page.Width.Point, 20), XStringFormats.TopCenter);
            y += 22;
            gfx.DrawString(title, fontBold, XBrushes.Black, new XRect(0, y, page.Width.Point, 16), XStringFormats.TopCenter);
            y += 18;
            if (!string.IsNullOrEmpty(subtitle))
            {
                gfx.DrawString(subtitle, fontNormal, XBrushes.Gray, new XRect(0, y, page.Width.Point, 14), XStringFormats.TopCenter);
                y += 18;
            }
            y += 4;

            var headerRect = new XRect(margin, y, totalW, rowH);
            gfx.DrawRectangle(XPens.Gray, XBrushes.LightGray, headerRect);
            double x = margin;
            for (int i = 0; i < columns.Count; i++)
            {
                gfx.DrawString(Truncate(columns[i].Header, 15), fontBold, XBrushes.Black, new XRect(x, y, cols[i], rowH), XStringFormats.CenterLeft);
                x += cols[i];
            }
            y += rowH;
        }

        EnsurePage();
        foreach (var row in rowsList)
        {
            EnsurePage();
            double x = margin;
            for (int i = 0; i < columns.Count; i++)
            {
                var val = columns[i].GetValue(row) ?? "";
                gfx!.DrawString(Truncate(val, 20), fontNormal, XBrushes.Black, new XRect(x, y, cols[i], rowH), XStringFormats.CenterLeft);
                x += cols[i];
            }
            y += rowH;
        }

        gfx?.Dispose();
        doc.Save(filePath);
        doc.Dispose();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private static string Truncate(string value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= maxLen ? value : value[..maxLen] + "...";
    }
}
