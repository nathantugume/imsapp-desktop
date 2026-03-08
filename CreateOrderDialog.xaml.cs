using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop;

public sealed partial class CreateOrderDialog : ContentDialog
{
    private readonly ObservableCollection<OrderLineItem> _items = new();

    public bool OrderCreated { get; private set; }

    public CreateOrderDialog()
    {
        InitializeComponent();
        OrderDate.Date = DateTimeOffset.Now;
        ItemsList.ItemsSource = _items;
        LoadProducts();
        Loaded += (_, _) => UpdateTotals();
    }

    private async void LoadProducts()
    {
        var products = await ServiceLocator.Products.GetAllAsync();
        ProductCombo.ItemsSource = products;
        if (products.Count > 0)
            ProductCombo.SelectedIndex = 0;
    }

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        if (ProductCombo.SelectedItem is not Product p)
        {
            ErrorText.Text = "Select a product.";
            return;
        }
        var qty = (int)QtyBox.Value;
        if (qty <= 0 || qty > p.Stock)
        {
            ErrorText.Text = $"Invalid quantity. Stock: {p.Stock}";
            return;
        }
        var existing = _items.FirstOrDefault(x => x.Pid == p.Pid);
        if (existing != null)
        {
            var newQty = existing.Quantity + qty;
            if (newQty > p.Stock)
            {
                ErrorText.Text = $"Total quantity exceeds stock ({p.Stock}).";
                return;
            }
            existing.Quantity = newQty;
            existing.SetPrices(p.Price, (double)(p.WholesalePrice ?? (decimal)p.Price));
        }
        else
        {
            var wholesalePrice = (double)(p.WholesalePrice ?? (decimal)p.Price);
            _items.Add(new OrderLineItem
            {
                Pid = p.Pid,
                ProductName = p.ProductName,
                Quantity = qty,
                RetailPrice = p.Price,
                WholesalePrice = wholesalePrice,
                UseWholesale = false,
                Stock = p.Stock
            });
        }
        ErrorText.Text = "";
        UpdateTotals();
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is OrderLineItem item)
        {
            _items.Remove(item);
            UpdateTotals();
        }
    }

    private void PriceMode_Changed(object sender, RoutedEventArgs e) => UpdateTotals();

    private void Totals_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) => UpdateTotals();

    private void UpdateTotals()
    {
        var subtotal = _items.Sum(x => x.Quantity * x.PricePerItem);
        var gst = 0.0;
        var discount = DiscountBox?.Value ?? 0;
        var netTotal = Math.Max(0, subtotal - discount);
        var paid = PaidBox?.Value ?? 0;
        var due = Math.Max(0, netTotal - paid);
        var sym = ServiceLocator.Branding.Current.CurrencySymbol;
        if (SubTotalText != null) SubTotalText.Text = $"{sym} {subtotal:N2}";
        if (GstText != null) GstText.Text = $"{sym} {gst:N2}";
        if (DiscountText != null) DiscountText.Text = $"{sym} {discount:N2}";
        if (NetTotalText != null) NetTotalText.Text = $"{sym} {netTotal:N2}";
        if (PaidText != null) PaidText.Text = $"{sym} {paid:N2}";
        if (DueText != null) DueText.Text = $"{sym} {due:N2}";
    }

    private async void CreateOrderDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        var deferral = args.GetDeferral();

        try
        {
            if (string.IsNullOrWhiteSpace(CustomerName.Text))
            {
                ErrorText.Text = "Enter customer name.";
                return;
            }
            if (_items.Count == 0)
            {
                ErrorText.Text = "Add at least one item.";
                return;
            }

            var subtotal = _items.Sum(x => x.Quantity * x.PricePerItem);
            var discount = Math.Max(0, DiscountBox.Value);
            var netTotal = Math.Max(0, subtotal - discount);
            var paid = (decimal)Math.Max(0, PaidBox.Value);
            var due = (decimal)netTotal - paid;

            if (discount > subtotal)
            {
                ErrorText.Text = "Discount cannot exceed subtotal.";
                return;
            }

            var products = await ServiceLocator.Products.GetAllAsync();
            var productStock = products.ToDictionary(x => x.Pid, x => x.Stock);
            foreach (var item in _items)
            {
                var available = productStock.GetValueOrDefault(item.Pid, 0);
                if (item.Quantity > available)
                {
                    ErrorText.Text = $"Insufficient stock for {item.ProductName}. Available: {available}";
                    return;
                }
            }

            var request = new CreateOrderRequest
            {
                CustomerName = CustomerName.Text.Trim(),
                Address = string.IsNullOrWhiteSpace(Address.Text) ? "In-store" : Address.Text.Trim(),
                OrderDate = OrderDate.Date.ToString("dd-MM-yyyy"),
                PaymentMethod = (PaymentMethod.SelectedItem as string) ?? "Cash",
                Subtotal = subtotal,
                Gst = 0,
                Discount = discount,
                NetTotal = netTotal,
                Paid = paid,
                Due = due,
                Items = _items.Select(x => new OrderItemRequest
                {
                    Pid = x.Pid,
                    ProductName = x.ProductName,
                    Quantity = x.Quantity,
                    PricePerItem = x.PricePerItem,
                    Stock = productStock.GetValueOrDefault(x.Pid, 0)
                }).ToList()
            };

            var (success, invoiceNo, message) = await ServiceLocator.Orders.CreateAsync(request);
            if (success)
            {
                ErrorText.Text = "";
                OrderCreated = true;
                var xamlRoot = XamlRoot;
                Hide();
                DispatcherQueue.TryEnqueue(async () =>
                {
                    await Task.Delay(100);
                    var done = new ContentDialog
                    {
                        Title = "Success",
                        Content = $"Order #{invoiceNo} created.",
                        PrimaryButtonText = "Print Invoice",
                        CloseButtonText = "Close"
                    };
                    done.XamlRoot = xamlRoot;
                    var result = await done.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        var orderWithItems = await ServiceLocator.Orders.GetByIdWithItemsAsync(invoiceNo);
                        if (orderWithItems != null)
                        {
                            var invoiceDlg = new InvoiceDialog(orderWithItems);
                            invoiceDlg.XamlRoot = xamlRoot;
                            await invoiceDlg.ShowAsync();
                        }
                    }
                });
            }
            else
            {
                ErrorText.Text = message;
            }
        }
        finally
        {
            deferral.Complete();
        }
    }
}

public class OrderLineItem : System.ComponentModel.INotifyPropertyChanged
{
    public int Pid { get; set; }
    public string ProductName { get; set; } = "";
    private int _quantity;
    public int Quantity { get => _quantity; set { _quantity = value; RaisePropertyChanged(); } }
    public double RetailPrice { get; set; }
    public double WholesalePrice { get; set; }
    private bool _useWholesale;
    public bool UseWholesale { get => _useWholesale; set { _useWholesale = value; RaisePropertyChanged(); } }
    public double PricePerItem => UseWholesale ? WholesalePrice : RetailPrice;
    public int Stock { get; set; }
    public string LineTotalFormatted => ServiceLocator.Branding.Current.FormatCurrency(Quantity * PricePerItem);
    public string PriceModeLabel => UseWholesale ? "W" : "R";
    public bool HasWholesaleOption => Math.Abs(RetailPrice - WholesalePrice) > 0.001;

    public void SetPrices(double retail, double wholesale) { RetailPrice = retail; WholesalePrice = wholesale; RaisePropertyChanged(); }

    private void RaisePropertyChanged()
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Quantity)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(UseWholesale)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(PricePerItem)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(LineTotalFormatted)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(PriceModeLabel)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(HasWholesaleOption)));
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}
