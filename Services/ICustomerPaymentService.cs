using System.Collections.Generic;
using System.Threading.Tasks;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public interface ICustomerPaymentService
{
    Task<IReadOnlyList<OutstandingOrder>> GetOutstandingOrdersAsync();
    Task<IReadOnlyList<CustomerPayment>> GetPaymentHistoryAsync(int? invoiceNo = null);
    Task<(bool Success, string Message)> RecordPaymentAsync(int invoiceNo, decimal amountPaid, string paymentMethod, string? notes);
}
