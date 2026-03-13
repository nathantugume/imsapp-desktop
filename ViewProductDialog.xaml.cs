using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class ViewProductDialog : ContentDialog
{
    public ViewProductDialog(Product product)
    {
        InitializeComponent();
        var sym = ServiceLocator.Branding.Current.CurrencySymbol;
        ContentPanel.Children.Add(MakeRow("Product ID", product.Pid.ToString()));
        ContentPanel.Children.Add(MakeRow("Product Name", product.ProductName));
        ContentPanel.Children.Add(MakeRow("Category", product.CategoryName ?? "—"));
        ContentPanel.Children.Add(MakeRow("Brand", product.BrandName ?? "—"));
        ContentPanel.Children.Add(MakeRow("Supplier", product.SupplierName ?? "—"));
        ContentPanel.Children.Add(MakeRow("Stock", product.Stock.ToString()));
        ContentPanel.Children.Add(MakeRow("Unit", product.SaleUnitDisplay));
        ContentPanel.Children.Add(MakeRow("Retail Price", $"{sym} {product.Price:N2}"));
        ContentPanel.Children.Add(MakeRow("Wholesale Price", product.WholesalePrice.HasValue && product.WholesalePrice > 0 ? $"{sym} {product.WholesalePrice:N2}" : "—"));
        ContentPanel.Children.Add(MakeRow("Buying Price", $"{sym} {product.BuyingPrice:N2}"));
        ContentPanel.Children.Add(MakeRow("Status", product.StatusDisplay));
        ContentPanel.Children.Add(MakeRow("Description", string.IsNullOrEmpty(product.Description) ? "—" : product.Description));
        if (product.ExpiryDate.HasValue)
            ContentPanel.Children.Add(MakeRow("Expiry Date", product.ExpiryDate.Value.ToString("yyyy-MM-dd")));
    }

    private static StackPanel MakeRow(string label, string value)
    {
        var panel = new StackPanel { Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 12) };
        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
        });
        panel.Children.Add(new TextBlock { Text = value, Margin = new Microsoft.UI.Xaml.Thickness(0, 2, 0, 0), TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap });
        return panel;
    }
}
