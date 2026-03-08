using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class SupplierEditDialog : ContentDialog
{
    private readonly Supplier? _supplier;

    public SupplierEditDialog(Supplier? supplier)
    {
        InitializeComponent();
        _supplier = supplier;
        Title = supplier == null ? "Add Supplier" : "Edit Supplier";

        if (supplier != null)
        {
            SupplierNameBox.Text = supplier.SupplierName;
            ContactPersonBox.Text = supplier.ContactPerson ?? "";
            PhoneBox.Text = supplier.Phone ?? "";
            EmailBox.Text = supplier.Email ?? "";
            AddressBox.Text = supplier.Address ?? "";
            StatusCombo.SelectedIndex = supplier.Status == "1" ? 0 : 1;
        }
    }

    private async void Save_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        if (string.IsNullOrWhiteSpace(SupplierNameBox.Text))
        {
            args.Cancel = true;
            deferral.Complete();
            return;
        }

        var s = _supplier ?? new Supplier();
        s.SupplierName = SupplierNameBox.Text.Trim();
        s.ContactPerson = string.IsNullOrWhiteSpace(ContactPersonBox.Text) ? null : ContactPersonBox.Text.Trim();
        s.Phone = string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim();
        s.Email = string.IsNullOrWhiteSpace(EmailBox.Text) ? null : EmailBox.Text.Trim();
        s.Address = string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text.Trim();
        s.Status = StatusCombo.SelectedIndex == 0 ? "1" : "0";

        try
        {
            if (_supplier == null)
                await ServiceLocator.Suppliers.AddAsync(s);
            else
                await ServiceLocator.Suppliers.UpdateAsync(s);
        }
        catch (Exception ex)
        {
            args.Cancel = true;
            var dlg = new ContentDialog { Title = "Error", Content = ex.Message, CloseButtonText = "OK" };
            dlg.XamlRoot = XamlRoot;
            await dlg.ShowAsync();
        }
        deferral.Complete();
    }
}
