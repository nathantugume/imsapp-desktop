# Export in Different Formats – Implementation Guide

## How It Works in the Previous (PHP) Project

- **Reports** (`reports.php`): DataTables buttons – **Export Excel**, **Export PDF**, **Export CSV** (and Print). Uses DataTables Buttons extension + pdfmake for PDF, built-in CSV/Excel.
- **Invoices**: FPDF generates invoice PDFs (`orders/generate_pdf.php`).
- **Database**: CLI export (full/structure/data) via `migrations/export-database.php`.

## How It’s Done in the Desktop App

### Reports page (already implemented)

- **UI**: Export button with `MenuFlyout` → "Export CSV", "Export Excel", "Export PDF".
- **Flow**:
  1. User picks format and clicks (e.g. Export CSV).
  2. `FileSavePicker` asks where to save (e.g. `Report_Daily_2025-03-07.csv`).
  3. `ReportExportService.ExportToCsv` / `ExportToExcel` / `ExportToPdf` writes the file.
  4. Success dialog: "Exported N rows to filename".

- **Libraries**:
  - **CSV**: Plain `StringBuilder` + `FileIO.WriteTextAsync`.
  - **Excel**: **ClosedXML** (`XLWorkbook`, worksheets, cells).
  - **PDF**: **PDFsharp** (same as invoice PDFs; tables with `XGraphics`).

- **Code locations**:
  - `ReportsPage.xaml` – Export button + `MenuFlyoutItem` (CSV / Excel / PDF).
  - `ReportsPage.xaml.cs` – `ExportCsv_Click`, `ExportExcel_Click`, `ExportPdf_Click`, `PickSaveFileAsync`, `ShowExportSuccessAsync`.
  - `Services/ReportExportService.cs` – `ExportToCsv`, `ExportToExcel`, `ExportToPdf` for `ReportRow[]`.

### FileSavePicker (required for WinUI)

```csharp
var picker = new FileSavePicker
{
    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
    SuggestedFileName = "Products_2025-03-07",
    FileTypeChoices = { { "CSV file", new[] { ".csv" } } }  // or "Excel workbook", ".xlsx" / "PDF", ".pdf"
};
InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindowInstance));
var file = await picker.PickSaveFileAsync();
// file.Path for Excel/PDF; FileIO.WriteTextAsync(file, csv) for CSV
```

---

## Adding Export to Other Pages (Products, Orders, etc.)

### Option A – Page-specific export (like Reports)

For each list (Products, Orders, Categories, Brands, Suppliers):

1. **Add UI**: An "Export" button with a `MenuFlyout` and three items: "Export CSV", "Export Excel", "Export PDF".
2. **Add methods**: e.g. `ExportCsv_Click`, `ExportExcel_Click`, `ExportPdf_Click` that:
   - Get current data from the ViewModel (e.g. `Products`, `Orders`).
   - If count is 0, show "No data to export".
   - Call `PickSaveFileAsync` with the right extension (`.csv`, `.xlsx`, `.pdf`).
   - Call an export service that knows that entity’s columns and rows.
   - Call `ShowExportSuccessAsync(rowCount, file.Name)`.
3. **Implement export logic** for that entity:
   - **CSV**: Header line + one line per row, comma-separated; escape quotes in strings.
   - **Excel**: `ClosedXML` – one worksheet, header row, then data rows.
   - **PDF**: `PDFsharp` – title + table (header + rows), optional extra pages if many rows.

### Option B – Generic export helper

Use a shared helper that takes:

- Title (e.g. "Products", "Orders").
- Optional subtitle/period (e.g. "2025-03-07" or "Daily Report").
- Column headers (string array).
- Rows (e.g. `IReadOnlyList<string[]>` or a small row type).

Then one implementation can export to CSV, Excel, and PDF for any grid. Each page only builds the column list and row list from its ViewModel and calls the helper.

See `Services/DataExportHelper.cs` for a generic implementation you can reuse.

**Example – exporting Products from ProductListPage:**

```csharp
// In ProductListPage.xaml: add Export button with MenuFlyout (Export CSV / Excel / PDF), same as ReportsPage.

// In code-behind, get data and build rows (example for products):
var products = ((ProductListViewModel)DataContext).Products;  // or your VM property
if (products.Count == 0) { ShowError("No data to export"); return; }

var headers = new[] { "Name", "Category", "Brand", "Unit", "Buying", "Price", "Stock", "Status" };
var rows = products.Select(p => new[]
{
    p.Name ?? "",
    p.CategoryName ?? "",
    p.BrandName ?? "",
    p.Unit ?? "",
    p.BuyingPrice.ToString("N2"),
    p.Price.ToString("N2"),
    p.StockQuantity.ToString(),
    p.Status ?? ""
}).ToList();

// CSV
var csv = DataExportHelper.ExportToCsv(headers, rows);
await FileIO.WriteTextAsync(saveFile, csv);

// Excel / PDF (need file path from FileSavePicker)
DataExportHelper.ExportToExcel("Products", DateTime.Now.ToString("yyyy-MM-dd"), headers, rows, filePath);
// or run PDF on background: await Task.Run(() => DataExportHelper.ExportToPdf(...));
```

Use the same `PickSaveFileAsync` / `ShowExportSuccessAsync` pattern as in `ReportsPage.xaml.cs`.

---

## Suggested export targets

| Page / data       | Suggested columns (examples) |
|-------------------|-------------------------------|
| **Products**      | Name, Category, Brand, Supplier, Unit, Buying, Price, Wholesale, Stock, Status |
| **Orders**        | Invoice No, Customer, Date, Payment, Subtotal, Discount, Net, Paid, Due |
| **Categories**    | Name, Description, Status |
| **Brands**        | Name, Description, Status |
| **Suppliers**     | Name, Contact, Phone, Email, Address, Status |
| **Reports**       | Already done (Invoice, Customer, Subtotal, GST, Discount, Net, Paid, Due, Payment, Date, ItemCount) |

---

## Summary

- **Reports** already export to **CSV, Excel, and PDF** like the PHP project.
- Reuse the same pattern (Export button → MenuFlyout → FileSavePicker → export service → success message) on other pages.
- Use **ClosedXML** for Excel and **PDFsharp** for PDF everywhere; CSV is just text.
- Add either page-specific export methods per list or a **generic DataExportHelper** and call it from each page with the right columns and rows.
