using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class ViewOrderDialog : ContentDialog
{
    public ViewOrderDialog(int invoiceNo)
    {
        InitializeComponent();
        LoadOrder(invoiceNo);
    }

    private async void LoadOrder(int invoiceNo)
    {
        var orderWithItems = await ServiceLocator.Orders.GetByIdWithItemsAsync(invoiceNo);
        if (orderWithItems == null)
        {
            ContentPanel.Children.Add(new TextBlock { Text = "Order not found." });
            return;
        }

        var o = orderWithItems.Order;
        ContentPanel.Children.Add(new TextBlock { Text = $"Invoice #: {o.InvoiceNo}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        ContentPanel.Children.Add(new TextBlock { Text = $"Customer: {o.CustomerName}", Margin = new Microsoft.UI.Xaml.Thickness(0, 4, 0, 0) });
        ContentPanel.Children.Add(new TextBlock { Text = $"Address: {o.Address}", Margin = new Microsoft.UI.Xaml.Thickness(0, 2, 0, 0) });
        ContentPanel.Children.Add(new TextBlock { Text = $"Date: {o.OrderDate}", Margin = new Microsoft.UI.Xaml.Thickness(0, 2, 0, 0) });
        var sym = ServiceLocator.Branding.Current.CurrencySymbol;
        ContentPanel.Children.Add(new TextBlock { Text = $"Net Total: {sym} {o.NetTotal:N2}", Margin = new Microsoft.UI.Xaml.Thickness(0, 2, 0, 0) });
        ContentPanel.Children.Add(new TextBlock { Text = $"Paid: {sym} {o.Paid:N2} | Due: {sym} {o.Due:N2}", Margin = new Microsoft.UI.Xaml.Thickness(0, 2, 0, 0) });
        ContentPanel.Children.Add(new TextBlock { Text = "Items:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Microsoft.UI.Xaml.Thickness(0, 12, 0, 4) });

        foreach (var item in orderWithItems.Items)
        {
            ContentPanel.Children.Add(new TextBlock
            {
                Text = $"  {item.ProductName} x {item.OrderQty} @ {sym} {item.PricePerItem:N2} = {sym} {(item.OrderQty * item.PricePerItem):N2}",
                Margin = new Microsoft.UI.Xaml.Thickness(0, 2, 0, 0)
            });
        }
    }
}
