using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using imsapp_desktop.Models;
using imsapp_desktop.Services;

namespace imsapp_desktop.ViewModels;

public partial class ProductListViewModel : ObservableObject
{
    private readonly IProductService _productService = ServiceLocator.Products;
    private readonly ICategoryService _categoryService = ServiceLocator.Categories;
    private readonly IBrandService _brandService = ServiceLocator.Brands;
    private readonly ISupplierService _supplierService = ServiceLocator.Suppliers;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private Product? _selectedProduct;

    public bool HasSelection => SelectedProduct != null;

    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Brand> Brands { get; } = new();
    public ObservableCollection<Supplier> Suppliers { get; } = new();

    [ObservableProperty] private Category? _selectedCategoryFilter;
    [ObservableProperty] private Brand? _selectedBrandFilter;
    [ObservableProperty] private Supplier? _selectedSupplierFilter;
    [ObservableProperty] private string _selectedStatusFilter = "All";

    private List<Product> _allProducts = new();

    public ProductListViewModel()
    {
        Categories.Add(new Category { CatId = 0, CategoryName = "All Categories" });
        Brands.Add(new Brand { BrandId = 0, BrandName = "All Brands" });
        Suppliers.Add(new Supplier { SupplierId = 0, SupplierName = "All Suppliers" });
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var products = await _productService.GetAllAsync();
            var categories = await _categoryService.GetAllAsync();
            var brands = await _brandService.GetAllAsync();
            var suppliers = await _supplierService.GetAllAsync();

            Categories.Clear();
            Categories.Add(new Category { CatId = 0, CategoryName = "All Categories" });
            foreach (var c in categories) Categories.Add(c);

            Brands.Clear();
            Brands.Add(new Brand { BrandId = 0, BrandName = "All Brands" });
            foreach (var b in brands) Brands.Add(b);

            Suppliers.Clear();
            Suppliers.Add(new Supplier { SupplierId = 0, SupplierName = "All Suppliers" });
            foreach (var s in suppliers) Suppliers.Add(s);

            _allProducts = products.ToList();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to load: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allProducts.AsEnumerable();

        if (SelectedCategoryFilter != null && SelectedCategoryFilter.CatId != 0)
            filtered = filtered.Where(p => p.CatId == SelectedCategoryFilter.CatId);
        if (SelectedBrandFilter != null && SelectedBrandFilter.BrandId != 0)
            filtered = filtered.Where(p => p.BrandId == SelectedBrandFilter.BrandId);
        if (SelectedSupplierFilter != null && SelectedSupplierFilter.SupplierId != 0)
            filtered = filtered.Where(p => p.SupplierId == SelectedSupplierFilter.SupplierId);
        if (!string.IsNullOrEmpty(SelectedStatusFilter) && SelectedStatusFilter != "All")
        {
            if (SelectedStatusFilter == "Active") filtered = filtered.Where(p => p.PStatus == "1");
            else if (SelectedStatusFilter == "Inactive") filtered = filtered.Where(p => p.PStatus == "0");
        }

        Products.Clear();
        foreach (var p in filtered) Products.Add(p);
    }

    partial void OnSelectedProductChanged(Product? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnSelectedCategoryFilterChanged(Category? value) { if (_allProducts.Count > 0) ApplyFilters(); }
    partial void OnSelectedBrandFilterChanged(Brand? value) { if (_allProducts.Count > 0) ApplyFilters(); }
    partial void OnSelectedSupplierFilterChanged(Supplier? value) { if (_allProducts.Count > 0) ApplyFilters(); }
    partial void OnSelectedStatusFilterChanged(string value) { if (_allProducts.Count > 0) ApplyFilters(); }

    [RelayCommand]
    private void ClearFilters()
    {
        SelectedCategoryFilter = Categories.FirstOrDefault(c => c.CatId == 0);
        SelectedBrandFilter = Brands.FirstOrDefault(b => b.BrandId == 0);
        SelectedSupplierFilter = Suppliers.FirstOrDefault(s => s.SupplierId == 0);
        SelectedStatusFilter = "All";
    }
}
