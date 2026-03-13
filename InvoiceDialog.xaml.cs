using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class InvoiceDialog : ContentDialog
{
    private OrderWithItems? _orderData;
    private string _htmlPath = string.Empty;

    public InvoiceDialog(OrderWithItems orderData)
    {
        InitializeComponent();
        _orderData = orderData;
        Loaded += (_, _) => RenderInvoice();
    }

    private void RenderInvoice()
    {
        if (_orderData == null) return;

        var b = ServiceLocator.Branding.Current;
        var o = _orderData.Order;
        var sym = b.CurrencySymbol.ToUpperInvariant();

        InvoicePanel.Children.Clear();

        var title = new TextBlock
        {
            Text = b.BusinessName,
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        };
        InvoicePanel.Children.Add(title);

        var contact = $"{b.BusinessAddress} | Tel: {b.BusinessPhone}";
        if (!string.IsNullOrEmpty(b.BusinessEmail)) contact += $" | Email: {b.BusinessEmail}";
        InvoicePanel.Children.Add(new TextBlock { Text = contact, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) });

        InvoicePanel.Children.Add(new TextBlock { Text = $"Customer: {o.CustomerName}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        InvoicePanel.Children.Add(new TextBlock { Text = $"Address: {o.Address}", Margin = new Thickness(0, 2, 0, 0) });
        InvoicePanel.Children.Add(new TextBlock { Text = $"Order Date: {o.OrderDate}  |  Invoice No: SIN/{o.InvoiceNo}", Margin = new Thickness(0, 4, 0, 8), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (int i = 0; i <= _orderData.Items.Count; i++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddHeaderCell(grid, 0, 0, "S.N.");
        AddHeaderCell(grid, 1, 0, "Product");
        AddHeaderCell(grid, 2, 0, "Qty");
        AddHeaderCell(grid, 3, 0, $"Total ({sym})");

        for (int i = 0; i < _orderData.Items.Count; i++)
        {
            var item = _orderData.Items[i];
            var total = item.OrderQty * (double)item.PricePerItem;
            AddCell(grid, 0, i + 1, (i + 1).ToString());
            AddCell(grid, 1, i + 1, item.ProductName);
            AddCell(grid, 2, i + 1, item.OrderQty.ToString());
            AddCell(grid, 3, i + 1, total.ToString("N2"));
        }

        InvoicePanel.Children.Add(grid);

        var totalsPanel = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        totalsPanel.Children.Add(new TextBlock { Text = $"Subtotal: {sym} {o.Subtotal:N2}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        totalsPanel.Children.Add(new TextBlock { Text = $"GST: {sym} {o.Gst:N2}", Margin = new Thickness(0, 2, 0, 0) });
        totalsPanel.Children.Add(new TextBlock { Text = $"Discount: {sym} {o.Discount:N2}", Margin = new Thickness(0, 2, 0, 0) });
        totalsPanel.Children.Add(new TextBlock { Text = $"Net Total: {sym} {o.NetTotal:N2}", FontWeight = Microsoft.UI.Text.FontWeights.Bold, Margin = new Thickness(0, 4, 0, 0) });
        totalsPanel.Children.Add(new TextBlock { Text = $"Paid: {sym} {o.Paid:N2}  |  Due: {sym} {o.Due:N2}", Margin = new Thickness(0, 2, 0, 0) });
        totalsPanel.Children.Add(new TextBlock { Text = $"Payment: {o.PaymentMethod}", Margin = new Thickness(0, 2, 0, 0) });
        totalsPanel.Children.Add(new TextBlock { Text = "------------------------------------------", Margin = new Thickness(0, 12, 0, 4), FontSize = 10 });
        totalsPanel.Children.Add(new TextBlock { Text = "Authorized Signature", FontSize = 10, HorizontalAlignment = HorizontalAlignment.Right });
        InvoicePanel.Children.Add(totalsPanel);
    }

    private static void AddHeaderCell(Grid grid, int col, int row, string text)
    {
        var tb = new TextBlock { Text = text, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Margin = new Thickness(4, 4, 4, 4) };
        Grid.SetColumn(tb, col);
        Grid.SetRow(tb, row);
        grid.Children.Add(tb);
    }

    private static void AddCell(Grid grid, int col, int row, string text)
    {
        var tb = new TextBlock { Text = text, Margin = new Thickness(4, 4, 4, 4) };
        Grid.SetColumn(tb, col);
        Grid.SetRow(tb, row);
        grid.Children.Add(tb);
    }

    private async void Print_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        if (_orderData == null) return;

        PrintMessageBar.Message = "Generating PDF...";
        PrintMessageBar.Severity = InfoBarSeverity.Informational;
        PrintMessageBar.IsOpen = true;

        try
        {
            var pdfPath = await Task.Run(() => InvoicePdfService.GenerateAndSave(_orderData));
            var fullPath = Path.GetFullPath(pdfPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("PDF was not created.", fullPath);
            Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
            PrintMessageBar.Message = "PDF generated and opened. Press Ctrl+P in the PDF viewer to print.";
            PrintMessageBar.Severity = InfoBarSeverity.Success;
        }
        catch (Exception ex)
        {
            try
            {
                var html = BuildInvoiceHtml();
                var tempDir = Path.Combine(Path.GetTempPath(), "imsapp-desktop");
                Directory.CreateDirectory(tempDir);
                _htmlPath = Path.Combine(tempDir, $"invoice_{_orderData.Order.InvoiceNo}.html");
                await File.WriteAllTextAsync(_htmlPath, html, Encoding.UTF8);
                Process.Start(new ProcessStartInfo(Path.GetFullPath(_htmlPath)) { UseShellExecute = true });
                PrintMessageBar.Message = "PDF failed. Opened HTML in browser. Press Ctrl+P to print.";
                PrintMessageBar.Severity = InfoBarSeverity.Warning;
            }
            catch
            {
                PrintMessageBar.Message = "PDF failed: " + ex.Message;
                PrintMessageBar.Severity = InfoBarSeverity.Error;
            }
        }
    }

    private string BuildInvoiceHtml()
    {
        if (_orderData == null) return "";
        var b = ServiceLocator.Branding.Current;
        var o = _orderData.Order;
        var sym = b.CurrencySymbol.ToUpperInvariant();

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Invoice ").Append(o.InvoiceNo).Append("</title>");
        sb.Append("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;max-width:600px}table{width:100%;border-collapse:collapse}");
        sb.Append("th,td{border:1px solid #333;padding:8px;text-align:left}th{background:#eee}.right{text-align:right}");
        sb.Append("h1{text-align:center;margin-bottom:4px}.contact{text-align:center;font-size:12px;margin-bottom:16px}");
        sb.Append(".totals{margin-top:16px}.sig{margin-top:24px;border-top:1px solid #333;padding-top:8px;text-align:right;font-size:12px}</style></head><body>");

        sb.Append("<h1>").Append(System.Net.WebUtility.HtmlEncode(b.BusinessName)).Append("</h1>");
        var contact = $"{b.BusinessAddress} | Tel: {b.BusinessPhone}";
        if (!string.IsNullOrEmpty(b.BusinessEmail)) contact += $" | Email: {b.BusinessEmail}";
        sb.Append("<p class='contact'>").Append(System.Net.WebUtility.HtmlEncode(contact)).Append("</p>");

        sb.Append("<p><b>Customer:</b> ").Append(System.Net.WebUtility.HtmlEncode(o.CustomerName)).Append("</p>");
        sb.Append("<p><b>Address:</b> ").Append(System.Net.WebUtility.HtmlEncode(o.Address)).Append("</p>");
        sb.Append("<p><b>Order Date:</b> ").Append(System.Net.WebUtility.HtmlEncode(o.OrderDate)).Append(" &nbsp;|&nbsp; <b>Invoice No:</b> SIN/").Append(o.InvoiceNo).Append("</p>");

        sb.Append("<table><thead><tr><th>S.N.</th><th>Product</th><th>Qty</th><th>Price</th><th>Total (").Append(sym).Append(")</th></tr></thead><tbody>");
        int sn = 1;
        foreach (var item in _orderData.Items)
        {
            var total = item.OrderQty * (double)item.PricePerItem;
            sb.Append("<tr><td>").Append(sn++).Append("</td><td>").Append(System.Net.WebUtility.HtmlEncode(item.ProductName))
              .Append("</td><td>").Append(item.OrderQty).Append("</td><td>").Append(((double)item.PricePerItem).ToString("N2"))
              .Append("</td><td class='right'>").Append(total.ToString("N2")).Append("</td></tr>");
        }
        sb.Append("</tbody></table>");

        sb.Append("<div class='totals'><p><b>Subtotal:</b> ").Append(sym).Append(" ").Append(o.Subtotal.ToString("N2")).Append("</p>");
        sb.Append("<p><b>GST:</b> ").Append(sym).Append(" ").Append(o.Gst.ToString("N2")).Append("</p>");
        sb.Append("<p><b>Discount:</b> ").Append(sym).Append(" ").Append(o.Discount.ToString("N2")).Append("</p>");
        sb.Append("<p><b>Net Total:</b> ").Append(sym).Append(" ").Append(o.NetTotal.ToString("N2")).Append("</p>");
        sb.Append("<p><b>Paid:</b> ").Append(sym).Append(" ").Append(o.Paid.ToString("N2")).Append(" &nbsp;|&nbsp; <b>Due:</b> ").Append(sym).Append(" ").Append(o.Due.ToString("N2")).Append("</p>");
        sb.Append("<p><b>Payment:</b> ").Append(System.Net.WebUtility.HtmlEncode(o.PaymentMethod)).Append("</p></div>");

        sb.Append("<div class='sig'>").Append(System.Net.WebUtility.HtmlEncode(b.BusinessName)).Append("<br>Authorized Signature</div>");
        sb.Append("</body></html>");
        return sb.ToString();
    }
}
