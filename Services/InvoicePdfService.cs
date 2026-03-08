using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop.Services;

public static class InvoicePdfService
{
    public static string GenerateAndSave(OrderWithItems orderData)
    {
        var dir = Path.Combine(Path.GetTempPath(), "imsapp-desktop", "Invoices");
        Directory.CreateDirectory(dir);
        var safeName = string.Join("_", orderData.Order.CustomerName.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(dir, $"invoice_{orderData.Order.InvoiceNo}_{safeName}.pdf");

        var doc = new PdfDocument();
        doc.Info.Title = $"Invoice {orderData.Order.InvoiceNo}";
        var page = doc.AddPage();
        page.Size = PdfSharp.PageSize.A4;

        using var gfx = XGraphics.FromPdfPage(page);
        var fontTitle = new XFont("Arial", 16, XFontStyleEx.Bold);
        var fontNormal = new XFont("Arial", 10);
        var fontSmall = new XFont("Arial", 9);
        var fontBold = new XFont("Arial", 10, XFontStyleEx.Bold);

        const double margin = 40;
        double y = margin;

        var b = ServiceLocator.Branding.Current;
        var o = orderData.Order;
        var sym = b.CurrencySymbol.ToUpperInvariant();

        // Header
        gfx.DrawString(b.BusinessName, fontTitle, XBrushes.Black, new XRect(0, y, page.Width, 24), XStringFormats.TopCenter);
        y += 24;

        var contact = $"{b.BusinessAddress} | Tel: {b.BusinessPhone}";
        if (!string.IsNullOrEmpty(b.BusinessEmail)) contact += $" | Email: {b.BusinessEmail}";
        gfx.DrawString(contact, fontSmall, XBrushes.Black, new XRect(0, y, page.Width, 16), XStringFormats.TopCenter);
        y += 24;

        // Customer
        gfx.DrawString($"Customer Name: {o.CustomerName}", fontBold, XBrushes.Black, margin, y);
        y += 16;
        gfx.DrawString($"Address: {o.Address}", fontNormal, XBrushes.Black, margin, y);
        y += 16;
        gfx.DrawString($"Order Date: {o.OrderDate}", fontNormal, XBrushes.Black, page.Width - margin - 120, y - 16);
        gfx.DrawString($"Invoice No: SIN/{o.InvoiceNo}", fontNormal, XBrushes.Black, page.Width - margin - 120, y);
        y += 20;

        // Table
        double colW0 = 30;
        double colW1 = page.Width - margin * 2 - colW0 - 50 - 60 - 70;
        double colW2 = 50;
        double colW3 = 60;
        double colW4 = 70;
        double rowH = 22;

        var headerRect = new XRect(margin, y, colW0 + colW1 + colW2 + colW3 + colW4, rowH);
        gfx.DrawRectangle(XPens.Gray, XBrushes.LightGray, headerRect);
        gfx.DrawString("S.N.", fontBold, XBrushes.Black, new XRect(margin, y, colW0, rowH), XStringFormats.CenterLeft);
        gfx.DrawString("Product", fontBold, XBrushes.Black, new XRect(margin + colW0, y, colW1, rowH), XStringFormats.CenterLeft);
        gfx.DrawString("Qty", fontBold, XBrushes.Black, new XRect(margin + colW0 + colW1, y, colW2, rowH), XStringFormats.CenterLeft);
        gfx.DrawString("Price", fontBold, XBrushes.Black, new XRect(margin + colW0 + colW1 + colW2, y, colW3, rowH), XStringFormats.CenterLeft);
        gfx.DrawString($"Total ({sym})", fontBold, XBrushes.Black, new XRect(margin + colW0 + colW1 + colW2 + colW3, y, colW4, rowH), XStringFormats.CenterRight);
        y += rowH;

        int sn = 1;
        foreach (var item in orderData.Items)
        {
            var total = item.OrderQty * (double)item.PricePerItem;
            var rowRect = new XRect(margin, y, colW0 + colW1 + colW2 + colW3 + colW4, rowH);
            gfx.DrawRectangle(XPens.LightGray, XBrushes.White, rowRect);
            gfx.DrawString(sn++.ToString(), fontNormal, XBrushes.Black, new XRect(margin, y, colW0, rowH), XStringFormats.CenterLeft);
            gfx.DrawString(item.ProductName, fontNormal, XBrushes.Black, new XRect(margin + colW0, y, colW1, rowH), XStringFormats.CenterLeft);
            gfx.DrawString(item.OrderQty.ToString(), fontNormal, XBrushes.Black, new XRect(margin + colW0 + colW1, y, colW2, rowH), XStringFormats.CenterLeft);
            gfx.DrawString(((double)item.PricePerItem).ToString("N2"), fontNormal, XBrushes.Black, new XRect(margin + colW0 + colW1 + colW2, y, colW3, rowH), XStringFormats.CenterLeft);
            gfx.DrawString(total.ToString("N2"), fontNormal, XBrushes.Black, new XRect(margin + colW0 + colW1 + colW2 + colW3, y, colW4, rowH), XStringFormats.CenterRight);
            y += rowH;
        }

        y += 20;

        // Totals
        double totX = page.Width - margin - 180;
        gfx.DrawString("Sub Total", fontNormal, XBrushes.Black, totX, y);
        gfx.DrawString(o.Subtotal.ToString("N2"), fontNormal, XBrushes.Black, totX + 100, y, XStringFormats.TopRight);
        y += 18;
        gfx.DrawString("GST", fontNormal, XBrushes.Black, totX, y);
        gfx.DrawString(o.Gst.ToString("N2"), fontNormal, XBrushes.Black, totX + 100, y, XStringFormats.TopRight);
        y += 18;
        gfx.DrawString("Discount", fontNormal, XBrushes.Black, totX, y);
        gfx.DrawString(o.Discount.ToString("N2"), fontNormal, XBrushes.Black, totX + 100, y, XStringFormats.TopRight);
        y += 18;
        gfx.DrawString("Net Total", fontBold, XBrushes.Black, totX, y);
        gfx.DrawString(o.NetTotal.ToString("N2"), fontBold, XBrushes.Black, totX + 100, y, XStringFormats.TopRight);
        y += 18;
        gfx.DrawString("Paid", fontNormal, XBrushes.Black, totX, y);
        gfx.DrawString(o.Paid.ToString("N2"), fontNormal, XBrushes.Black, totX + 100, y, XStringFormats.TopRight);
        y += 18;
        gfx.DrawString("Due", fontNormal, XBrushes.Black, totX, y);
        gfx.DrawString(o.Due.ToString("N2"), fontNormal, XBrushes.Black, totX + 100, y, XStringFormats.TopRight);
        y += 18;
        gfx.DrawString("Payment", fontNormal, XBrushes.Black, totX, y);
        gfx.DrawString(o.PaymentMethod, fontNormal, XBrushes.Black, totX + 100, y, XStringFormats.TopRight);
        y += 32;

        // Signature
        gfx.DrawString(b.BusinessName, fontNormal, XBrushes.Black, page.Width - margin, y, XStringFormats.TopRight);
        y += 14;
        gfx.DrawString("------------------------------------------", fontSmall, XBrushes.Black, page.Width - margin, y, XStringFormats.TopRight);
        y += 14;
        gfx.DrawString("Authorized Signature", fontSmall, XBrushes.Black, page.Width - margin, y, XStringFormats.TopRight);

        doc.Save(path);
        doc.Dispose();

        return path;
    }
}
