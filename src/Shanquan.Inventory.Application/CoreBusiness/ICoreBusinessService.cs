namespace Shanquan.Inventory.Application.CoreBusiness;

public interface ICoreBusinessService
{
    Task<BusinessWorkspace> GetWorkspaceAsync(Guid? customerId = null, CancellationToken cancellationToken = default);
    Task<Guid> SaveCustomerAsync(SaveCustomerRequest request, CancellationToken cancellationToken = default);
    Task SetCustomerActiveAsync(Guid customerId, bool isActive, CancellationToken cancellationToken = default);
    Task<Guid> SaveWarehouseAsync(string code, string name, CancellationToken cancellationToken = default);
    Task<Guid> SaveWarehouseAsync(Guid? warehouseId, string code, string name, CancellationToken cancellationToken = default);
    Task DeleteWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task SaveCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task RenameCategoryAsync(string currentName, string newName, CancellationToken cancellationToken = default);
    Task SaveCategoriesAsync(IReadOnlyList<string> categories, CancellationToken cancellationToken = default);
    Task<CategoryUsage> GetCategoryUsageAsync(string category, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<Guid> SaveColorAsync(string chineseName, string englishName, string hex, CancellationToken cancellationToken = default);
    Task<Guid> SaveColorNameAsync(Guid? colorId, string name, CancellationToken cancellationToken = default);
    Task DeleteColorAsync(Guid colorId, CancellationToken cancellationToken = default);
    Task<Guid> SaveProductAsync(SaveProductRequest request, CancellationToken cancellationToken = default);
    Task<Guid> AddSkuAsync(AddSkuRequest request, CancellationToken cancellationToken = default);
    Task SaveSkuAsync(SaveSkuRequest request, CancellationToken cancellationToken = default);
    Task<string> ReceiveStockAsync(ReceiveStockRequest request, CancellationToken cancellationToken = default);
    Task<string> AdjustStockAsync(AdjustStockRequest request, CancellationToken cancellationToken = default);
    Task<SalesDraftData?> GetSalesDraftAsync(CancellationToken cancellationToken = default);
    Task SaveSalesDraftAsync(SaveSalesDraftRequest request, CancellationToken cancellationToken = default);
    Task ClearSalesDraftAsync(CancellationToken cancellationToken = default);
    Task<string> CreateAndReserveSalesOrderAsync(CreateSalesOrderRequest request, CancellationToken cancellationToken = default);
    Task StartPickingAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
    Task CompletePickingAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
    Task<string> ShipSalesOrderAsync(Guid salesOrderId, string? expressTrackingNumber = null, CancellationToken cancellationToken = default);
    Task ConfirmSalesOrderReceiptAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
    Task<string> RecordCustomerPaymentAsync(RecordCustomerPaymentRequest request, CancellationToken cancellationToken = default);
}

public sealed record BusinessWorkspace(
    IReadOnlyList<CustomerListItem> Customers,
    IReadOnlyList<WarehouseListItem> Warehouses,
    IReadOnlyList<string> Categories,
    IReadOnlyList<ColorListItem> Colors,
    IReadOnlyList<ProductListItem> Products,
    IReadOnlyList<SkuListItem> Skus,
    IReadOnlyList<SalesOrderListItem> SalesOrders,
    IReadOnlyList<ShippingOrderLineItem> ShippingOrderLines,
    IReadOnlyList<StockListItem> Stock,
    IReadOnlyList<StockMovementListItem> StockMovements,
    IReadOnlyList<ReceivableListItem> Receivables,
    IReadOnlyList<OpenReceivableListItem> OpenReceivables);

public sealed record CustomerListItem(
    Guid Id,
    string Code,
    string Name,
    string Phone,
    string Address,
    long CreditLimitCents,
    long ReceivableCents,
    long TotalSalesCents,
    long MonthlySalesCents,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastTransactionAt,
    bool IsActive,
    int PaymentTermDays)
{
    public string DisplayName => $"{Code} · {Name}";
    public string CustomerSubtext => $"{Code} · 批发A级";
    public string ContactText => string.IsNullOrWhiteSpace(Phone) ? "未填写联系电话" : $"联系人 · {Phone}";
    public string ReceivableText => $"¥{ReceivableCents / 100m:N0}";
    public string CreditLimitText => $"¥{CreditLimitCents / 100m:N0}";
    public string AvailableCreditText => $"可用 ¥{Math.Max(0, CreditLimitCents - ReceivableCents) / 100m:N0}";
    public string TotalSalesText => $"¥{TotalSalesCents / 100m:N0}";
    public bool HasReceivable => ReceivableCents > 0;
    public bool HasNoReceivable => !HasReceivable;
    public string StatusText => IsActive ? "启用" : "停用";
    public string PaymentTermText => $"账期 {PaymentTermDays} 天";
    public string LastTransactionText
    {
        get
        {
            if (LastTransactionAt is null) return "暂无交易";
            var local = LastTransactionAt.Value.LocalDateTime;
            var today = DateTime.Today;
            if (local.Date == today) return $"今天 {local:HH:mm}";
            if (local.Date == today.AddDays(-1)) return $"昨天 {local:HH:mm}";
            return local.ToString("MM-dd HH:mm");
        }
    }
}

public sealed record WarehouseListItem(Guid Id, string Code, string Name)
{
    public string DisplayName => $"{Code} · {Name}";
}

public sealed record ColorListItem(Guid Id, string ChineseName, string EnglishName, string Hex)
{
    public string DisplayName => ChineseName;
    public string Value => ChineseName;
}

public sealed record CategoryUsage(string Category, int ProductCount, bool HasSalesData);

public sealed record ProductListItem(
    Guid Id,
    string StyleNumber,
    string Name,
    string Category,
    string Season,
    long BasePriceCents,
    long CostPriceCents,
    bool IsActive)
{
    public string DisplayName => $"{StyleNumber} · {Name}";
    public string BasePriceText => MoneyText.Format(BasePriceCents);
}

public sealed record SkuListItem(
    Guid Id,
    Guid ProductId,
    string StyleNumber,
    string ProductName,
    string ColorCode,
    string SizeCode,
    string Barcode,
    long BasePriceCents,
    long EffectivePriceCents,
    long OnHand,
    long Reserved)
{
    public long Available => OnHand - Reserved;
    public string DisplayName => $"{StyleNumber} · {ProductName} · {ColorCode}/{SizeCode}";
    public string PriceText => MoneyText.Format(EffectivePriceCents);
    public string StockText => $"可用 {Available}";
}

public sealed record SalesOrderListItem(
    Guid Id,
    Guid CustomerId,
    string Number,
    string CustomerName,
    string WarehouseName,
    string Status,
    long TotalQuantity,
    long TotalAmountCents,
    DateTimeOffset DocumentDate,
    DateTimeOffset CreatedAt,
    string PaymentStatus = "未收款",
    DateTimeOffset? ApprovedAt = null,
    DateTimeOffset? PickingStartedAt = null,
    DateTimeOffset? ReadyToShipAt = null,
    DateTimeOffset? ShippedAt = null,
    DateTimeOffset? SignedAt = null,
    string ExpressTrackingNumber = "")
{
    public string QuantityText => $"{TotalQuantity} 件";
    public string AmountText => MoneyText.Format(TotalAmountCents);
    public string CreatedAtText => CreatedAt.LocalDateTime.ToString("MM-dd HH:mm");
    public bool CanStartPicking => Status is "待配货" or "已锁库";
    public bool CanShip => Status == "待发货";
    public bool CanSign => Status == "已发货";
    public bool IsActiveFulfillment => Status is "待配货" or "已锁库" or "配货中" or "待发货";
    public string DisplayStatus => Status == "已锁库" ? "待配货" : Status;
    public string PaymentStatusText => PaymentStatus;
    public bool HasExpressTrackingNumber => !string.IsNullOrWhiteSpace(ExpressTrackingNumber);
    public string ExpressTrackingNumberText => $"快递单号：{ExpressTrackingNumber}";
    public DateTimeOffset BusinessTime => SignedAt ?? ShippedAt ?? ReadyToShipAt ?? PickingStartedAt ?? ApprovedAt ?? CreatedAt;
    public string BusinessTimeText => BusinessTime.LocalDateTime.Date == DateTime.Today
        ? $"今天 {BusinessTime.LocalDateTime:HH:mm}"
        : BusinessTime.LocalDateTime.ToString("MM-dd HH:mm");
    public string StatusBackground => DisplayStatus switch
    {
        "已签收" => "#E7F3ED",
        "已发货" => "#E8F0F8",
        "待发货" => "#FFF1DA",
        "配货中" => "#E8F0F8",
        _ => "#FAE8E9",
    };
    public string StatusForeground => DisplayStatus switch
    {
        "已签收" => "#318A65",
        "已发货" or "配货中" => "#477EA4",
        "待发货" => "#B77720",
        _ => "#9D3037",
    };
}

public sealed record ShippingOrderLineItem(
    Guid SalesOrderId,
    Guid SalesOrderLineId,
    Guid SkuId,
    string StyleNumber,
    string ProductName,
    string ColorCode,
    string SizeCode,
    string Barcode,
    long Quantity,
    long UnitPriceCents,
    long AmountCents)
{
    public string VariantText => $"{ColorCode}/{SizeCode}";
    public string QuantityText => $"{Quantity} 件";
    public string UnitPriceText => MoneyText.Format(UnitPriceCents);
    public string AmountText => MoneyText.Format(AmountCents);
}

public sealed record StockListItem(
    Guid SkuId,
    Guid WarehouseId,
    string WarehouseName,
    string StyleNumber,
    string ProductName,
    string Category,
    string Season,
    string ColorCode,
    string SizeCode,
    long OnHand,
    long Reserved)
{
    public long Available => OnHand - Reserved;
    public string Variant => $"{ColorCode}/{SizeCode}";
}

public sealed record StockMovementListItem(
    string ReferenceNumber,
    string MovementType,
    string WarehouseName,
    string StyleNumber,
    string Variant,
    long OnHandChange,
    long ReservedChange,
    long OnHandAfter,
    long ReservedAfter,
    DateTimeOffset CreatedAt)
{
    public string ChangeText => OnHandChange > 0 ? $"+{OnHandChange}" : OnHandChange.ToString();
    public string CreatedAtText => CreatedAt.LocalDateTime.ToString("MM-dd HH:mm");
}

public sealed record ReceivableListItem(
    Guid CustomerId,
    string CustomerName,
    string EntryType,
    string ReferenceNumber,
    long AmountCents,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DueAt,
    string PaymentMethod,
    string Note)
{
    public string AmountText => MoneyText.Format(AmountCents);
    public string CreatedAtText => CreatedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
    public string DirectionText => AmountCents < 0 ? "收款" : "应收";
}

public sealed record OpenReceivableListItem(
    Guid CustomerId,
    string CustomerName,
    string ReferenceNumber,
    long OutstandingCents,
    DateTimeOffset CreatedAt,
    DateTimeOffset DueAt)
{
    public int OverdueDays => Math.Max(0, (DateTime.Today - DueAt.LocalDateTime.Date).Days);
    public bool IsOverdue => DueAt.LocalDateTime.Date < DateTime.Today;
    public bool IsDueWithinSevenDays => DueAt.LocalDateTime.Date >= DateTime.Today && DueAt.LocalDateTime.Date <= DateTime.Today.AddDays(7);
}

public sealed record CreateSalesOrderRequest(
    Guid CustomerId,
    Guid WarehouseId,
    IReadOnlyList<CreateSalesOrderLine> Lines,
    DateTimeOffset DocumentDate,
    long FreightCents = 0,
    long DiscountCents = 0,
    string PaymentStatus = "未收款");

public sealed record CreateSalesOrderLine(Guid SkuId, long Quantity, long? UnitPriceCents = null);

public sealed record SaveSalesDraftRequest(
    Guid CustomerId,
    Guid WarehouseId,
    IReadOnlyList<CreateSalesOrderLine> Lines,
    DateTimeOffset DocumentDate,
    long FreightCents,
    long DiscountCents,
    string PaymentStatus);

public sealed record SalesDraftData(
    Guid CustomerId,
    Guid WarehouseId,
    IReadOnlyList<CreateSalesOrderLine> Lines,
    DateTimeOffset DocumentDate,
    long FreightCents,
    long DiscountCents,
    string PaymentStatus,
    DateTimeOffset UpdatedAt);

public sealed record SaveCustomerRequest(
    Guid? Id,
    string Code,
    string Name,
    string Phone,
    string Address,
    long CreditLimitCents,
    int PaymentTermDays = 30);

public sealed record RecordCustomerPaymentRequest(
    Guid CustomerId,
    long AmountCents,
    DateTimeOffset ReceivedAt,
    string PaymentMethod,
    string Note);

public sealed record SaveProductRequest(
    Guid? Id,
    string StyleNumber,
    string Name,
    string Category,
    string Season,
    long BasePriceCents,
    long CostPriceCents);

public sealed record AddSkuRequest(Guid ProductId, string ColorCode, string SizeCode, string Barcode, Guid? WarehouseId = null);
public sealed record SaveSkuRequest(Guid SkuId, string ColorCode, string SizeCode, string Barcode);
public sealed record ReceiveStockRequest(Guid WarehouseId, Guid SkuId, long Quantity, string Note);
public sealed record AdjustStockRequest(Guid WarehouseId, Guid SkuId, long TargetOnHand, string Reason);

public static class MoneyText
{
    public static string Format(long cents) => $"¥{cents / 100m:N2}";
}
