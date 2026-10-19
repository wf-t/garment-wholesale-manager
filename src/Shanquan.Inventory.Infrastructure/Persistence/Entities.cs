namespace Shanquan.Inventory.Infrastructure.Persistence;

public sealed class ProductEntity
{
    public Guid Id { get; set; }
    public string StyleNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long BasePriceCents { get; set; }
    public long CostPriceCents { get; set; }
    public string? Category { get; set; }
    public string? Season { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SkuEntity
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ColorCode { get; set; } = string.Empty;
    public string SizeCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
}

public sealed class CustomerEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long CreditLimitCents { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int PaymentTermDays { get; set; } = 30;
}

public sealed class WarehouseEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class CategoryEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ColorEntity
{
    public Guid Id { get; set; }
    public string ChineseName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string Hex { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class StockBalanceEntity
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid SkuId { get; set; }
    public long OnHand { get; set; }
    public long Reserved { get; set; }
    public int Version { get; set; }
}

public sealed class SalesOrderEntity
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid WarehouseId { get; set; }
    public string Status { get; set; } = string.Empty;
    public long TotalQuantity { get; set; }
    public long GoodsAmountCents { get; set; }
    public long FreightCents { get; set; }
    public long DiscountCents { get; set; }
    public long TotalAmountCents { get; set; }
    public long ReceivedAmountCents { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTimeOffset DocumentDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? PickingStartedAt { get; set; }
    public DateTimeOffset? ReadyToShipAt { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public DateTimeOffset? SignedAt { get; set; }
}

public sealed class SalesDraftEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid WarehouseId { get; set; }
    public long FreightCents { get; set; }
    public long DiscountCents { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTimeOffset DocumentDate { get; set; }
    public string LinesJson { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class SalesOrderLineEntity
{
    public Guid Id { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid SkuId { get; set; }
    public long Quantity { get; set; }
    public long ShippedQuantity { get; set; }
    public long UnitPriceCents { get; set; }
    public long AmountCents { get; set; }
}

public sealed class ShipmentEntity
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid WarehouseId { get; set; }
    public long TotalQuantity { get; set; }
    public long TotalAmountCents { get; set; }
    public string? ExpressTrackingNumber { get; set; }
    public DateTimeOffset ShippedAt { get; set; }
}

public sealed class ShipmentLineEntity
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid SalesOrderLineId { get; set; }
    public Guid SkuId { get; set; }
    public long Quantity { get; set; }
}

public sealed class StockMovementEntity
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid SkuId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public long OnHandChange { get; set; }
    public long ReservedChange { get; set; }
    public long OnHandAfter { get; set; }
    public long ReservedAfter { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ReceivableEntryEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public long AmountCents { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Note { get; set; }
}
