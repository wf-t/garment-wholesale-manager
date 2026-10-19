using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Shanquan.Inventory.Application.CoreBusiness;
using Shanquan.Inventory.Desktop.Services;
using Shanquan.Inventory.Desktop.ViewModels.Dialogs;

namespace Shanquan.Inventory.Desktop.Views.Dialogs;

public partial class InventoryFormWindow : Window
{
    private readonly ICoreBusinessService? _businessService;
    private bool _selectingProduct;
    public InventoryFormWindow() : this(null, InventoryFormMode.Product, [], [], [], [], [], [], null, null) { }

    public InventoryFormWindow(
        ICoreBusinessService? businessService,
        InventoryFormMode mode,
        IReadOnlyList<ProductListItem> products,
        IReadOnlyList<WarehouseListItem> warehouses,
        IReadOnlyList<string> categories,
        IReadOnlyList<ColorListItem> colors,
        IReadOnlyList<SkuListItem> skus,
        IReadOnlyList<StockListItem> stock,
        ProductListItem? product,
        SkuListItem? sku)
    {
        _businessService = businessService;
        InitializeComponent();
        DataContext = new InventoryFormViewModel(businessService, mode, products, warehouses, categories, colors, skus, stock, product, sku);
    }

    private void ProductSearchInput_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_selectingProduct || DataContext is not InventoryFormViewModel viewModel) return;
        if (viewModel.SelectedProduct is not null
            && string.Equals(ProductSearchInput.Text, viewModel.SelectedProduct.DisplayName, StringComparison.Ordinal))
        {
            ProductSearchPanel.IsVisible = false;
            return;
        }
        if (string.IsNullOrWhiteSpace(ProductSearchInput.Text))
        {
            ProductSearchPanel.IsVisible = false;
            return;
        }
        RefreshProductResults(viewModel, ProductSearchInput.Text, true);
    }

    private void ProductSearchInput_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ProductSearchInput.Text) && DataContext is InventoryFormViewModel viewModel)
            RefreshProductResults(viewModel, ProductSearchInput.Text, true);
    }

    private void ToggleProductDropDown_Click(object? sender, RoutedEventArgs e)
    {
        if (ProductSearchPanel.IsVisible)
        {
            ProductSearchPanel.IsVisible = false;
            return;
        }
        if (DataContext is not InventoryFormViewModel viewModel) return;
        RefreshProductResults(viewModel, ProductSearchInput.Text, true);
        ProductSearchInput.Focus();
    }

    private void ProductSearchInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ProductSearchPanel.IsVisible = false;
            e.Handled = true;
            return;
        }
        if (e.Key != Key.Enter || DataContext is not InventoryFormViewModel viewModel) return;
        if (viewModel.FilteredProducts.FirstOrDefault() is { } first)
        {
            SelectProduct(viewModel, first);
            e.Handled = true;
        }
    }

    private void SelectProduct_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ProductListItem product } && DataContext is InventoryFormViewModel viewModel)
            SelectProduct(viewModel, product);
    }

    private void SelectProduct(InventoryFormViewModel viewModel, ProductListItem product)
    {
        _selectingProduct = true;
        viewModel.SelectedProduct = product;
        viewModel.ProductSearchText = product.DisplayName;
        ProductSearchInput.Text = product.DisplayName;
        ProductSearchPanel.IsVisible = false;
        _selectingProduct = false;
    }

    private void StockProductSearchInput_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_selectingProduct || DataContext is not InventoryFormViewModel viewModel) return;
        if (viewModel.SelectedProduct is not null
            && string.Equals(StockProductSearchInput.Text, viewModel.SelectedProduct.DisplayName, StringComparison.Ordinal))
        {
            StockProductSearchPanel.IsVisible = false;
            return;
        }
        if (string.IsNullOrWhiteSpace(StockProductSearchInput.Text))
        {
            StockProductSearchPanel.IsVisible = false;
            return;
        }
        RefreshStockProductResults(viewModel, StockProductSearchInput.Text, true);
    }

    private void StockProductSearchInput_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (DataContext is InventoryFormViewModel viewModel)
            RefreshStockProductResults(viewModel, StockProductSearchInput.Text, true);
    }

    private void ToggleStockProductDropDown_Click(object? sender, RoutedEventArgs e)
    {
        if (StockProductSearchPanel.IsVisible)
        {
            StockProductSearchPanel.IsVisible = false;
            return;
        }
        if (DataContext is not InventoryFormViewModel viewModel) return;
        RefreshStockProductResults(viewModel, StockProductSearchInput.Text, true);
        StockProductSearchInput.Focus();
    }

    private void StockProductSearchInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            StockProductSearchPanel.IsVisible = false;
            e.Handled = true;
            return;
        }
        if (e.Key != Key.Enter || DataContext is not InventoryFormViewModel viewModel) return;
        if (viewModel.FilteredProducts.FirstOrDefault() is { } first)
        {
            SelectStockProduct(viewModel, first);
            e.Handled = true;
        }
    }

    private void SelectStockProduct_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ProductListItem product } && DataContext is InventoryFormViewModel viewModel)
            SelectStockProduct(viewModel, product);
    }

    private void SelectStockProduct(InventoryFormViewModel viewModel, ProductListItem product)
    {
        _selectingProduct = true;
        viewModel.SelectedProduct = product;
        viewModel.ProductSearchText = product.DisplayName;
        StockProductSearchInput.Text = product.DisplayName;
        StockProductSearchPanel.IsVisible = false;
        _selectingProduct = false;
    }

    private void RefreshStockProductResults(InventoryFormViewModel viewModel, string? searchText, bool open)
    {
        var count = viewModel.FilterProducts(searchText);
        StockProductResultCount.Text = $"{count} 条结果";
        if (open) StockProductSearchPanel.IsVisible = true;
    }

    private void IncrementStock_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: StockSkuLine row } && DataContext is InventoryFormViewModel viewModel)
            viewModel.IncrementStockRow(row);
    }

    private void DecrementStock_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: StockSkuLine row } && DataContext is InventoryFormViewModel viewModel)
            viewModel.DecrementStockRow(row);
    }

    private void RefreshProductResults(InventoryFormViewModel viewModel, string? searchText, bool open)
    {
        var count = viewModel.FilterProducts(searchText);
        ProductResultCount.Text = $"{count} 条结果";
        if (open) ProductSearchPanel.IsVisible = true;
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is InventoryFormViewModel viewModel && viewModel.BuildResult() is { } result)
            Close(result);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void RemoveVariant_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ProductVariantEntry entry } && DataContext is InventoryFormViewModel viewModel)
            viewModel.RemoveVariant(entry);
    }

    private void IncrementVariant_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ProductVariantEntry entry } && DataContext is InventoryFormViewModel viewModel)
            viewModel.IncrementVariant(entry);
    }

    private void DecrementVariant_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ProductVariantEntry entry } && DataContext is InventoryFormViewModel viewModel)
            viewModel.DecrementVariant(entry);
    }

    private async void ManageWarehouses_Click(object? sender, RoutedEventArgs e)
    {
        if (_businessService is null || DataContext is not InventoryFormViewModel viewModel) return;
        var workspace = await _businessService.GetWorkspaceAsync();
        await new WarehouseManagementWindow(_businessService, workspace.Warehouses).ShowDialog(this);
        await viewModel.RefreshWarehousesAsync();
    }

    private async void ManageCategories_Click(object? sender, RoutedEventArgs e)
    {
        if (_businessService is null || DataContext is not InventoryFormViewModel viewModel) return;
        var workspace = await _businessService.GetWorkspaceAsync();
        await new CategoryManagementWindow(_businessService, workspace.Categories).ShowDialog(this);
        await viewModel.RefreshCategoriesAsync();
    }

    private async void ManageColors_Click(object? sender, RoutedEventArgs e)
    {
        if (_businessService is null || DataContext is not InventoryFormViewModel viewModel) return;
        var workspace = await _businessService.GetWorkspaceAsync();
        await new ColorManagementWindow(_businessService, workspace.Colors).ShowDialog(this);
        await viewModel.RefreshColorsAsync();
    }
}
