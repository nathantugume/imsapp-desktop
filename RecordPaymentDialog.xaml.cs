using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class RecordPaymentDialog : ContentDialog
{
    private readonly OutstandingOrder _order;

    public RecordPaymentDialog(OutstandingOrder order)
    {
        InitializeComponent();
        _order = order;
        OrderInfoText.Text = $"Invoice #{order.InvoiceNo} - {order.CustomerName}\nDue: {order.DueFormatted}";
        AmountBox.Header = $"Amount ({ServiceLocator.Branding.Current.CurrencySymbol})";
        AmountBox.Maximum = (double)order.Due;
        AmountBox.Value = (double)order.Due;
    }

    private async void Record_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        var amount = (decimal)AmountBox.Value;
        if (amount <= 0)
        {
            args.Cancel = true;
            deferral.Complete();
            return;
        }

        var method = MethodCombo.SelectedItem as string ?? "Cash";
        var (success, message) = await ServiceLocator.CustomerPayments.RecordPaymentAsync(_order.InvoiceNo, amount, method, NotesBox.Text?.Trim());

        if (!success)
        {
            args.Cancel = true;
            var dlg = new ContentDialog { Title = "Error", Content = message, CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
        }
        deferral.Complete();
    }
}
