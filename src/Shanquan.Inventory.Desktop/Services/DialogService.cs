using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Shanquan.Inventory.Application.CoreBusiness;
using Shanquan.Inventory.Desktop.Views.Dialogs;

namespace Shanquan.Inventory.Desktop.Services;

public sealed class DialogService(ICoreBusinessService businessService) : IDialogService
{
    public Task<CustomerFormData?> ShowCustomerFormAsync(CustomerListItem? customer)
        => new CustomerFormWindow(customer).ShowDialog<CustomerFormData?>(GetOwner());

    public Task<CustomerPaymentFormData?> ShowCustomerPaymentFormAsync(IReadOnlyList<CustomerListItem> customers, CustomerListItem? customer = null)
        => new CustomerPaymentFormWindow(customers, customer).ShowDialog<CustomerPaymentFormData?>(GetOwner());

    public Task ShowCustomerStatementAsync(CustomerStatementData data)
        => new CustomerStatementWindow(data).ShowDialog(GetOwner());

    public Task<bool> ShowConfirmAsync(string title, string message, string confirmText)
        => new ConfirmWindow(title, message, confirmText).ShowDialog<bool>(GetOwner());

    public async Task<string?> ShowShippingConfirmAsync(string title, string message, string confirmText)
    {
        var window = new ConfirmWindow(title, message, confirmText, "快递单号（选填）", "请输入快递单号");
        return await window.ShowDialog<bool>(GetOwner()) ? window.InputText : null;
    }

    public Task ShowSalesOrderPreviewAsync(SalesOrderPreviewData data)
        => new SalesOrderPreviewWindow(data).ShowDialog(GetOwner());

    public Task<InventoryFormData?> ShowInventoryFormAsync(
        InventoryFormMode mode,
        IReadOnlyList<ProductListItem> products,
        IReadOnlyList<WarehouseListItem> warehouses,
        IReadOnlyList<string> categories,
        IReadOnlyList<ColorListItem> colors,
        IReadOnlyList<SkuListItem> skus,
        IReadOnlyList<StockListItem> stock,
        ProductListItem? product = null,
        SkuListItem? sku = null)
        => new InventoryFormWindow(businessService, mode, products, warehouses, categories, colors, skus, stock, product, sku)
            .ShowDialog<InventoryFormData?>(GetOwner());

    private static Avalonia.Controls.Window GetOwner()
        => (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
           ?? throw new InvalidOperationException("主窗口尚未初始化。");
}
