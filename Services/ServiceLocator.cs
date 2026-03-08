namespace imsapp_desktop.Services;

/// <summary>
/// Simple service locator. Services are created once.
/// </summary>
public static class ServiceLocator
{
    private static IAuthService? _auth;
    private static IProductService? _products;
    private static ICategoryService? _categories;
    private static IBrandService? _brands;
    private static ISupplierService? _suppliers;
    private static IDashboardService? _dashboard;
    private static IOrderService? _orders;
    private static IStockReconciliationService? _stockReconciliation;
    private static ICustomerPaymentService? _customerPayments;
    private static IReportService? _reports;
    private static IBrandingService? _branding;
    private static IUserService? _users;

    public static IAuthService Auth => _auth ??= new AuthService();
    public static IUserService Users => _users ??= new UserService();
    public static IProductService Products => _products ??= new ProductService();
    public static ICategoryService Categories => _categories ??= new CategoryService();
    public static IBrandService Brands => _brands ??= new BrandService();
    public static ISupplierService Suppliers => _suppliers ??= new SupplierService();
    public static IDashboardService Dashboard => _dashboard ??= new DashboardService();
    public static IOrderService Orders => _orders ??= new OrderService();
    public static IStockReconciliationService StockReconciliation => _stockReconciliation ??= new StockReconciliationService();
    public static ICustomerPaymentService CustomerPayments => _customerPayments ??= new CustomerPaymentService();
    public static IReportService Reports => _reports ??= new ReportService();
    public static IBrandingService Branding => _branding ??= new BrandingService();
}
