using Shanquan.Inventory.Application.CoreBusiness;

namespace Shanquan.Inventory.Desktop.Services;

public interface IDialogService
{
    Task<CustomerFormData?> ShowCustomerFormAsync(CustomerListItem? customer);
    Task<CustomerPaymentFormData?> ShowCustomerPaymentFormAsync(IReadOnlyList<CustomerListItem> customers, CustomerListItem? customer = null);
    Task ShowCustomerStatementAsync(CustomerStatementData data);
    Task<bool> ShowConfirmAsync(string title, string message, string confirmText);
    Task<string?> ShowShippingConfirmAsync(string title, string message, string confirmText);
    Task ShowSalesOrderPreviewAsync(SalesOrderPreviewData data);
    Task<InventoryFormData?> ShowInventoryFormAsync(
        InventoryFormMode mode,
        IReadOnlyList<ProductListItem> products,
        IReadOnlyList<WarehouseListItem> warehouses,
        IReadOnlyList<string> categories,
        IReadOnlyList<ColorListItem> colors,
        IReadOnlyList<SkuListItem> skus,
        IReadOnlyList<StockListItem> stock,
        ProductListItem? product = null,
        SkuListItem? sku = null);
}

public sealed record CustomerFormData(
    Guid? Id,
    string Code,
    string Name,
    string Phone,
    string Address,
    string CreditLimitText,
    string PaymentTermDaysText);

public sealed record CustomerPaymentFormData(
    Guid CustomerId,
    string AmountText,
    DateTimeOffset ReceivedAt,
    string PaymentMethod,
    string Note);

public sealed record CustomerStatementData(
    string CustomerName,
    string PeriodText,
    string ReceivableText,
    IReadOnlyList<CustomerStatementLine> Lines);

public sealed record CustomerStatementLine(
    string DateText,
    string Type,
    string ReferenceNumber,
    string DebitText,
    string CreditText,
    string Note);

public sealed record SalesOrderPreviewData(
    string DraftNumber,
    string Customer,
    string Warehouse,
    string DocumentDate,
    string PaymentStatus,
    string GoodsAmount,
    string Freight,
    string Discount,
    string PayableAmount,
    IReadOnlyList<SalesOrderPreviewLine> Lines)
{
    public string CustomerLabel => $"客户：{Customer}";
    public string WarehouseLabel => $"仓库：{Warehouse}";
    public string DocumentDateLabel => $"日期：{DocumentDate}";
    public string PaymentStatusLabel => $"收款：{PaymentStatus}";
    public string GoodsAmountLabel => $"商品：{GoodsAmount}";
    public string FreightLabel => $"运费：{Freight}";
    public string DiscountLabel => $"优惠：{Discount}";
    public string PayableAmountLabel => $"应付：{PayableAmount}";

    public string ToPrintableText()
    {
        var rows = Lines.Select(x => $"{x.StyleNumber}\t{x.ProductName}\t{x.Color}\t{x.Size}\t{x.Quantity}\t{x.UnitPrice}\t{x.Amount}");
        return string.Join(Environment.NewLine,
        [
            "盛轩服饰 销售单预览",
            $"单号：{DraftNumber}",
            $"客户：{Customer}",
            $"仓库：{Warehouse}",
            $"日期：{DocumentDate}",
            $"收款状态：{PaymentStatus}",
            "",
            "款号\t商品\t颜色\t尺码\t数量\t单价\t金额",
            .. rows,
            "",
            $"商品金额：{GoodsAmount}  运费：{Freight}  优惠：{Discount}  应付：{PayableAmount}",
        ]);
    }
}

public sealed record SalesOrderPreviewLine(
    string StyleNumber,
    string ProductName,
    string Color,
    string Size,
    string Quantity,
    string UnitPrice,
    string Amount);

public enum InventoryFormMode
{
    Product,
    Sku,
    EditSku,
    Receive,
    Adjust,
}

public sealed record InventoryFormData(
    InventoryFormMode Mode,
    Guid? ProductId,
    Guid? WarehouseId,
    Guid? SkuId,
    string StyleNumber,
    string ProductName,
    string Category,
    string Season,
    string BasePriceText,
    string CostPriceText,
    IReadOnlyList<InventoryVariantFormData> Variants,
    string Barcode,
    string QuantityText,
    string Note,
    string SelectedWarehouseCode,
    IReadOnlyList<WarehouseFormData> Warehouses,
    IReadOnlyList<string> Categories,
    IReadOnlyList<InventoryStockLineFormData> StockLines);

public sealed record InventoryVariantFormData(string ColorCode, string SizeCode, long Quantity);
public sealed record InventoryStockLineFormData(Guid SkuId, long Quantity);
public sealed record WarehouseFormData(Guid? Id, string Code, string Name);
