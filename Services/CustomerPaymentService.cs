using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using imsapp_desktop.Data;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop.Services;

internal class OrderBalance { public decimal Due { get; set; } public decimal Paid { get; set; } }

public class CustomerPaymentService : ICustomerPaymentService
{
    private readonly IAuthService _auth = ServiceLocator.Auth;

    public async Task<IReadOnlyList<OutstandingOrder>> GetOutstandingOrdersAsync()
    {
        const string sql = @"
            SELECT invoice_no AS InvoiceNo, customer_name AS CustomerName, net_total AS NetTotal,
                paid AS Paid, due AS Due, order_date AS OrderDate
            FROM orders WHERE due > 0 ORDER BY order_date DESC";
        using var conn = DatabaseFactory.CreateConnection();
        var list = await conn.QueryAsync<OutstandingOrder>(sql);
        return list.ToList();
    }

    public async Task<IReadOnlyList<CustomerPayment>> GetPaymentHistoryAsync(int? invoiceNo = null)
    {
        string sql;
        if (invoiceNo.HasValue)
        {
            sql = @"
                SELECT cp.id AS Id, cp.invoice_no AS InvoiceNo, cp.amount_paid AS AmountPaid,
                    cp.payment_date AS PaymentDate, cp.payment_method AS PaymentMethod, cp.notes AS Notes,
                    cp.created_by AS CreatedBy, u.name AS CreatedByName
                FROM customer_payments cp
                LEFT JOIN users u ON cp.created_by = u.id
                WHERE cp.invoice_no = @InvoiceNo ORDER BY cp.payment_date DESC";
        }
        else
        {
            sql = @"
                SELECT cp.id AS Id, cp.invoice_no AS InvoiceNo, cp.amount_paid AS AmountPaid,
                    cp.payment_date AS PaymentDate, cp.payment_method AS PaymentMethod, cp.notes AS Notes,
                    cp.created_by AS CreatedBy, u.name AS CreatedByName, o.customer_name AS CustomerName
                FROM customer_payments cp
                LEFT JOIN users u ON cp.created_by = u.id
                JOIN orders o ON cp.invoice_no = o.invoice_no
                ORDER BY cp.payment_date DESC LIMIT 100";
        }

        using var conn = DatabaseFactory.CreateConnection();
        var list = await conn.QueryAsync<CustomerPayment>(sql, new { InvoiceNo = invoiceNo });
        return list.ToList();
    }

    public async Task<(bool Success, string Message)> RecordPaymentAsync(int invoiceNo, decimal amountPaid, string paymentMethod, string? notes)
    {
        var userId = _auth.CurrentUser?.Id ?? 0;
        if (userId == 0)
            return (false, "Not logged in.");

        if (amountPaid <= 0)
            return (false, "Amount must be greater than zero.");

        const string getOrderSql = "SELECT due AS Due, paid AS Paid FROM orders WHERE invoice_no = @InvoiceNo";
        using var conn = DatabaseFactory.CreateConnection();
        var order = await conn.QueryFirstOrDefaultAsync<OrderBalance>(getOrderSql, new { InvoiceNo = invoiceNo });
        if (order == null)
            return (false, "Order not found.");

        if (amountPaid > order.Due)
            return (false, "Amount exceeds amount due.");

        if (conn is MySqlConnector.MySqlConnection mysqlConn)
            await mysqlConn.OpenAsync();

        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(
                "INSERT INTO customer_payments (invoice_no, amount_paid, payment_method, notes, created_by) VALUES (@InvoiceNo, @AmountPaid, @PaymentMethod, @Notes, @CreatedBy)",
                new { InvoiceNo = invoiceNo, AmountPaid = amountPaid, PaymentMethod = paymentMethod, Notes = notes ?? "", CreatedBy = userId },
                tx);

            var newPaid = order.Paid + amountPaid;
            var newDue = order.Due - amountPaid;

            await conn.ExecuteAsync(
                "UPDATE orders SET paid = @Paid, due = @Due WHERE invoice_no = @InvoiceNo",
                new { Paid = newPaid, Due = newDue, InvoiceNo = invoiceNo },
                tx);

            tx.Commit();
            return (true, "Payment recorded.");
        }
        catch (System.Exception ex)
        {
            tx.Rollback();
            return (false, ex.Message);
        }
    }
}
