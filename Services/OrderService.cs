using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using imsapp_desktop.Data;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public class OrderService : IOrderService
{
    public async Task<IReadOnlyList<Order>> GetAllAsync()
    {
        const string sql = "SELECT invoice_no AS InvoiceNo, customer_name AS CustomerName, address AS Address, " +
            "subtotal AS Subtotal, gst AS Gst, discount AS Discount, net_total AS NetTotal, " +
            "paid AS Paid, due AS Due, payment_method AS PaymentMethod, order_date AS OrderDate " +
            "FROM orders ORDER BY invoice_no DESC";
        using var conn = DatabaseFactory.CreateConnection();
        var list = await conn.QueryAsync<Order>(sql);
        return list.ToList();
    }

    public async Task<OrderWithItems?> GetByIdWithItemsAsync(int invoiceNo)
    {
        const string orderSql = "SELECT invoice_no AS InvoiceNo, customer_name AS CustomerName, address AS Address, " +
            "subtotal AS Subtotal, gst AS Gst, discount AS Discount, net_total AS NetTotal, " +
            "paid AS Paid, due AS Due, payment_method AS PaymentMethod, order_date AS OrderDate " +
            "FROM orders WHERE invoice_no = @Id";
        const string itemsSql = "SELECT id AS Id, invoice_no AS InvoiceNo, product_name AS ProductName, " +
            "order_qty AS OrderQty, price_per_item AS PricePerItem, created_at AS CreatedAt " +
            "FROM invoices WHERE invoice_no = @Id";

        using var conn = DatabaseFactory.CreateConnection();
        var order = await conn.QueryFirstOrDefaultAsync<Order>(orderSql, new { Id = invoiceNo });
        if (order == null) return null;

        var items = (await conn.QueryAsync<Invoice>(itemsSql, new { Id = invoiceNo })).ToList();
        return new OrderWithItems { Order = order, Items = items };
    }

    public async Task<(bool Success, int InvoiceNo, string Message)> CreateAsync(CreateOrderRequest request)
    {
        foreach (var item in request.Items)
        {
            if (item.Quantity > item.Stock)
                return (false, 0, $"Insufficient stock for {item.ProductName}. Available: {item.Stock}");
        }

        var orderDate = string.IsNullOrEmpty(request.OrderDate)
            ? DateTime.Now.ToString("dd-MM-yyyy")
            : request.OrderDate;

        using var conn = DatabaseFactory.CreateConnection();
        if (conn is MySqlConnector.MySqlConnection mysqlConn)
            await mysqlConn.OpenAsync();

        using var tx = conn.BeginTransaction();
        try
        {
            const string insertOrder = @"INSERT INTO orders (customer_name, address, subtotal, gst, discount, net_total, paid, due, payment_method, order_date)
                VALUES (@CustomerName, @Address, @Subtotal, @Gst, @Discount, @NetTotal, @Paid, @Due, @PaymentMethod, @OrderDate);
                SELECT LAST_INSERT_ID();";

            var invoiceNo = await conn.ExecuteScalarAsync<int>(insertOrder, new
            {
                request.CustomerName,
                request.Address,
                request.Subtotal,
                request.Gst,
                request.Discount,
                request.NetTotal,
                request.Paid,
                request.Due,
                request.PaymentMethod,
                OrderDate = orderDate
            }, tx);

            foreach (var item in request.Items)
            {
                var remainingStock = item.Stock - item.Quantity;
                await conn.ExecuteAsync(
                    "UPDATE products SET stock = @Stock WHERE product_name = @ProductName",
                    new { Stock = remainingStock, item.ProductName }, tx);

                await conn.ExecuteAsync(
                    "INSERT INTO invoices (invoice_no, product_name, order_qty, price_per_item) VALUES (@InvoiceNo, @ProductName, @OrderQty, @PricePerItem)",
                    new { InvoiceNo = invoiceNo, item.ProductName, OrderQty = item.Quantity, PricePerItem = item.PricePerItem }, tx);
            }

            tx.Commit();
            return (true, invoiceNo, "Order created successfully.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, 0, ex.Message);
        }
    }
}
