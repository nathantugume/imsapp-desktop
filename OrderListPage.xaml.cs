using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Models;
using imsapp_desktop.Services;
using imsapp_desktop.ViewModels;

namespace imsapp_desktop;

public sealed partial class OrderListPage : Page
{
    public OrderListPage()
    {
        InitializeComponent();
    }

    private OrderListViewModel GetViewModel() => (OrderListViewModel)DataContext;

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        GetViewModel().LoadCommand.Execute(null);
    }

    private void OrderGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OrderGrid.SelectedItem is Order o)
            GetViewModel().SelectedOrder = o;
    }

    private async void CreateOrder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CreateOrderDialog();
        dialog.XamlRoot = XamlRoot;
        await dialog.ShowAsync();
        if (dialog.OrderCreated)
            await GetViewModel().LoadAsync();
    }

    private async void ViewOrder_Click(object sender, RoutedEventArgs e)
    {
        var vm = GetViewModel();
        if (vm.SelectedOrder == null) return;
        var dialog = new ViewOrderDialog(vm.SelectedOrder.InvoiceNo);
        dialog.XamlRoot = XamlRoot;
        await dialog.ShowAsync();
    }

    private async void PrintInvoice_Click(object sender, RoutedEventArgs e)
    {
        var vm = GetViewModel();
        if (vm.SelectedOrder == null) return;
        var orderWithItems = await ServiceLocator.Orders.GetByIdWithItemsAsync(vm.SelectedOrder.InvoiceNo);
        if (orderWithItems == null)
        {
            var err = new ContentDialog { Title = "Error", Content = "Order not found.", CloseButtonText = "OK" };
            err.XamlRoot = XamlRoot;
            await err.ShowAsync();
            return;
        }
        var dialog = new InvoiceDialog(orderWithItems);
        dialog.XamlRoot = XamlRoot;
        await dialog.ShowAsync();
    }

    private static readonly TableExportService.ExportColumn<Order>[] OrderColumns =
    {
        new("ID", o => o.InvoiceNo.ToString()),
        new("Customer", o => o.CustomerName),
        new("Total", o => o.NetTotalFormatted),
        new("Paid", o => o.PaidFormatted),
        new("Payment", o => o.PaymentMethod),
        new("Date", o => o.OrderDate)
    };

    private async void ExportCsv_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Orders", "Orders", GetViewModel().Orders.ToList(), OrderColumns, "csv");
    private async void ExportExcel_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Orders", "Orders", GetViewModel().Orders.ToList(), OrderColumns, "excel");
    private async void ExportPdf_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Orders", "Orders", GetViewModel().Orders.ToList(), OrderColumns, "pdf");
}
