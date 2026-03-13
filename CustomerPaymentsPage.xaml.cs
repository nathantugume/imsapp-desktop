using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Models;
using imsapp_desktop.Services;
using imsapp_desktop.ViewModels;

namespace imsapp_desktop;

public sealed partial class CustomerPaymentsPage : Page
{
    public CustomerPaymentsPage()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadCommand.Execute(null);
    }

    private void OutstandingGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OutstandingGrid.SelectedItem is OutstandingOrder o)
            ViewModel.SelectedOrder = o;
    }

    private async void RecordPayment_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedOrder == null) return;
        var dialog = new RecordPaymentDialog(ViewModel.SelectedOrder);
        dialog.XamlRoot = XamlRoot;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.LoadAsync();
    }

    private static readonly TableExportService.ExportColumn<OutstandingOrder>[] OutstandingColumns =
    {
        new("Invoice", o => o.InvoiceNo.ToString()),
        new("Customer", o => o.CustomerName),
        new("Total", o => o.NetTotalFormatted),
        new("Paid", o => o.PaidFormatted),
        new("Due", o => o.DueFormatted),
        new("Date", o => o.OrderDate)
    };

    private static readonly TableExportService.ExportColumn<CustomerPayment>[] HistoryColumns =
    {
        new("ID", p => p.Id.ToString()),
        new("Invoice", p => p.InvoiceNo.ToString()),
        new("Customer", p => p.CustomerName ?? ""),
        new("Amount", p => p.AmountFormatted),
        new("Method", p => p.PaymentMethod),
        new("Date", p => p.DateDisplay),
        new("By", p => p.CreatedByName ?? "")
    };

    private async void ExportOutstandingCsv_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Outstanding Orders", "OutstandingOrders", ViewModel.OutstandingOrders.ToList(), OutstandingColumns, "csv");
    private async void ExportOutstandingExcel_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Outstanding Orders", "OutstandingOrders", ViewModel.OutstandingOrders.ToList(), OutstandingColumns, "excel");
    private async void ExportOutstandingPdf_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Outstanding Orders", "OutstandingOrders", ViewModel.OutstandingOrders.ToList(), OutstandingColumns, "pdf");

    private async void ExportHistoryCsv_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Payment History", "PaymentHistory", ViewModel.PaymentHistory.ToList(), HistoryColumns, "csv");
    private async void ExportHistoryExcel_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Payment History", "PaymentHistory", ViewModel.PaymentHistory.ToList(), HistoryColumns, "excel");
    private async void ExportHistoryPdf_Click(object sender, RoutedEventArgs e) =>
        await ExportHelper.ExportTableAsync(XamlRoot, "Payment History", "PaymentHistory", ViewModel.PaymentHistory.ToList(), HistoryColumns, "pdf");
}
