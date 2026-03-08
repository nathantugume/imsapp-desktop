using System.Collections.Generic;
using System.Threading.Tasks;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public interface IOrderService
{
    Task<IReadOnlyList<Order>> GetAllAsync();
    Task<OrderWithItems?> GetByIdWithItemsAsync(int invoiceNo);
    Task<(bool Success, int InvoiceNo, string Message)> CreateAsync(CreateOrderRequest request);
}

public class OrderWithItems
{
    public Order Order { get; set; } = new();
    public List<Invoice> Items { get; set; } = new();
}

public class CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string Address { get; set; } = "In-store";
    public string OrderDate { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "Cash";
    public double Subtotal { get; set; }
    public double Gst { get; set; }
    public double Discount { get; set; }
    public double NetTotal { get; set; }
    public decimal Paid { get; set; }
    public decimal Due { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    public int Pid { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public double PricePerItem { get; set; }
    public int Stock { get; set; }
}
