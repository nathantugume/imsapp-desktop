using System.Data;
using Dapper;
using imsapp_desktop.Data;

namespace imsapp_desktop.Services;

public class DashboardService : IDashboardService
{
    public async Task<DashboardStats> GetStatsAsync()
    {
        using var conn = DatabaseFactory.CreateConnection();
        var users = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users");
        var categories = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM categories");
        var brands = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM brands");
        var products = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM products");
        var suppliers = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM suppliers");
        var orderCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM orders");

        var totalOrderValue = await conn.ExecuteScalarAsync<decimal?>("SELECT SUM(net_total) FROM orders") ?? 0;
        var cashReceived = await conn.ExecuteScalarAsync<decimal?>("SELECT SUM(paid) FROM orders WHERE payment_method = 'Cash'") ?? 0;
        var outstanding = await conn.ExecuteScalarAsync<decimal?>("SELECT SUM(due) FROM orders WHERE due > 0") ?? 0;

        var invRow = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT SUM(stock * COALESCE(unit_cost, buying_price, 0)) AS inventory_value, " +
            "SUM(stock * price) AS retail_value, " +
            "SUM(stock * COALESCE(wholesale_price, price)) AS wholesale_value " +
            "FROM products WHERE p_status = '1'");

        decimal invVal = 0, retailVal = 0, wholesaleVal = 0;
        if (invRow != null)
        {
            invVal = (decimal)(invRow.inventory_value ?? 0);
            retailVal = (decimal)(invRow.retail_value ?? 0);
            wholesaleVal = (decimal)(invRow.wholesale_value ?? 0);
        }

        return new DashboardStats
        {
            UserCount = users,
            CategoryCount = categories,
            BrandCount = brands,
            ProductCount = products,
            SupplierCount = suppliers,
            OrderCount = orderCount,
            TotalOrderValue = totalOrderValue,
            CashReceived = cashReceived,
            OutstandingBalance = outstanding,
            InventoryValue = invVal,
            RetailValue = retailVal,
            WholesaleValue = wholesaleVal
        };
    }

    public async Task<DailyProfitStats> GetTodayProfitAsync()
    {
        using var conn = DatabaseFactory.CreateConnection();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var rows = await conn.QueryAsync<InvoiceRow>(
            "SELECT i.product_name, i.order_qty, i.price_per_item, " +
            "COALESCE(p.unit_cost, p.buying_price) as cost_per_unit " +
            "FROM invoices i " +
            "INNER JOIN products p ON i.product_name = p.product_name " +
            "WHERE DATE(i.created_at) = @date AND (p.unit_cost > 0 OR p.buying_price > 0)",
            new { date = today });

        decimal revenue = 0, cost = 0;
        foreach (var r in rows)
        {
            revenue += (decimal)(r.order_qty * r.price_per_item);
            cost += (decimal)(r.order_qty * (r.cost_per_unit ?? 0));
        }

        return new DailyProfitStats
        {
            Date = today,
            Revenue = revenue,
            Cost = cost,
            Profit = revenue - cost,
            SalesCount = rows.Count()
        };
    }

    public async Task<MonthlyProfitStats> GetMonthlyProfitAsync()
    {
        using var conn = DatabaseFactory.CreateConnection();
        var now = DateTime.Now;
        var rows = await conn.QueryAsync<InvoiceRow>(
            "SELECT i.product_name, i.order_qty, i.price_per_item, " +
            "COALESCE(p.unit_cost, p.buying_price) as cost_per_unit " +
            "FROM invoices i INNER JOIN products p ON i.product_name = p.product_name " +
            "WHERE YEAR(i.created_at) = @year AND MONTH(i.created_at) = @month " +
            "AND (p.unit_cost > 0 OR p.buying_price > 0)",
            new { year = now.Year, month = now.Month });

        decimal revenue = 0, cost = 0;
        foreach (var r in rows)
        {
            revenue += (decimal)(r.order_qty * r.price_per_item);
            cost += (decimal)(r.order_qty * (r.cost_per_unit ?? 0));
        }
        return new MonthlyProfitStats
        {
            Year = now.Year,
            Month = now.Month,
            Revenue = revenue,
            Cost = cost,
            Profit = revenue - cost,
            SalesCount = rows.Count()
        };
    }

    public async Task<IReadOnlyList<ExpiryWarningItem>> GetExpiryWarningsAsync()
    {
        using var conn = DatabaseFactory.CreateConnection();
        var expired = await conn.QueryAsync<ExpiryRow>(
            "SELECT product_name, expiry_date, stock FROM products " +
            "WHERE expiry_date IS NOT NULL AND expiry_date < CURDATE() AND p_status = '1'");
        var expiring = await conn.QueryAsync<ExpiryRow>(
            "SELECT product_name, expiry_date, stock FROM products " +
            "WHERE expiry_date IS NOT NULL AND expiry_date <= DATE_ADD(CURDATE(), INTERVAL 30 DAY) " +
            "AND expiry_date >= CURDATE() AND p_status = '1' ORDER BY expiry_date ASC");

        var list = new List<ExpiryWarningItem>();
        var today = DateTime.Today;
        foreach (var r in expired)
        {
            list.Add(new ExpiryWarningItem
            {
                ProductName = r.product_name ?? "",
                ExpiryDate = r.expiry_date,
                Stock = r.stock,
                DaysLeft = 0,
                IsExpired = true
            });
        }
        foreach (var r in expiring)
        {
            var exp = r.expiry_date ?? today;
            var days = (int)Math.Ceiling((exp - today).TotalDays);
            list.Add(new ExpiryWarningItem
            {
                ProductName = r.product_name ?? "",
                ExpiryDate = r.expiry_date,
                Stock = r.stock,
                DaysLeft = days,
                IsExpired = false
            });
        }
        return list;
    }

    public async Task<IReadOnlyList<CurrentStockItem>> GetCurrentStockAsync()
    {
        using var conn = DatabaseFactory.CreateConnection();
        var rows = await conn.QueryAsync<StockRow>(
            "SELECT product_name, stock, price, buying_price FROM products WHERE p_status = '1' ORDER BY product_name");
        return rows.Select(r =>
        {
            var stock = r.stock;
            var avail = stock > 31 ? "In stock" : stock > 1 ? "Running out of stock" : "Out of stock";
            return new CurrentStockItem
            {
                ProductName = r.product_name ?? "",
                Stock = stock,
                Price = (decimal)r.price,
                Availability = avail,
                BuyingPrice = (decimal)r.buying_price
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<RecentSaleItem>> GetRecentSalesAsync()
    {
        using var conn = DatabaseFactory.CreateConnection();
        var rows = await conn.QueryAsync<SaleRow>(
            "SELECT o.customer_name, i.product_name, i.order_qty as quantity, o.paid, o.due, o.order_date " +
            "FROM orders o LEFT JOIN invoices i ON i.invoice_no = o.invoice_no " +
            "ORDER BY o.order_date DESC LIMIT 50");
        return rows.Select(r => new RecentSaleItem
        {
            CustomerName = r.customer_name ?? "N/A",
            ProductName = r.product_name ?? "N/A",
            Quantity = r.quantity,
            Paid = r.paid,
            Balance = r.due,
            OrderDate = FormatOrderDate(r.order_date)
        }).ToList();
    }

    private static string FormatOrderDate(string? d)
    {
        if (string.IsNullOrEmpty(d)) return "";
        if (DateTime.TryParse(d, out var dt)) return dt.ToString("MMM dd, yyyy");
        return d;
    }

    private class ExpiryRow { public string? product_name { get; set; } public DateTime? expiry_date { get; set; } public int stock { get; set; } }
    private class StockRow { public string? product_name { get; set; } public int stock { get; set; } public double price { get; set; } public double buying_price { get; set; } }
    private class SaleRow { public string? customer_name { get; set; } public string? product_name { get; set; } public int quantity { get; set; } public decimal paid { get; set; } public decimal due { get; set; } public string? order_date { get; set; } }

    private class InvoiceRow
    {
        public string product_name { get; set; } = "";
        public int order_qty { get; set; }
        public double price_per_item { get; set; }
        public decimal? cost_per_unit { get; set; }
    }
}
