using Shanquan.Inventory.Domain.Common;

namespace Shanquan.Inventory.Domain.Sales;

public enum SalesOrderStatus
{
    Draft,
    Approved,
    Reserved,
    PartiallyShipped,
    Completed,
    Cancelled,
}

public sealed record SalesOrderLine(Guid SkuId, long Quantity, Money UnitPrice)
{
    public Money Amount => UnitPrice * Quantity;
}

public sealed class SalesOrder
{
    private readonly List<SalesOrderLine> _lines = [];

    public SalesOrder(Guid customerId, Guid warehouseId)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("客户不能为空。", nameof(customerId));
        if (warehouseId == Guid.Empty) throw new ArgumentException("仓库不能为空。", nameof(warehouseId));
        Id = Guid.CreateVersion7();
        CustomerId = customerId;
        WarehouseId = warehouseId;
    }

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public Guid WarehouseId { get; }
    public SalesOrderStatus Status { get; private set; } = SalesOrderStatus.Draft;
    public IReadOnlyList<SalesOrderLine> Lines => _lines;
    public Money TotalAmount => _lines.Aggregate(Money.Zero, (sum, line) => sum + line.Amount);

    public void AddLine(Guid skuId, long quantity, Money unitPrice)
    {
        EnsureDraft();
        if (skuId == Guid.Empty) throw new ArgumentException("SKU不能为空。", nameof(skuId));
        if (quantity <= 0) throw new DomainRuleException("销售数量必须大于零。");
        if (unitPrice.Cents < 0) throw new DomainRuleException("销售单价不能小于零。");
        _lines.Add(new SalesOrderLine(skuId, quantity, unitPrice));
    }

    public void Approve()
    {
        EnsureDraft();
        if (_lines.Count == 0) throw new DomainRuleException("没有商品的销售单不能审核。");
        Status = SalesOrderStatus.Approved;
    }

    public void MarkReserved()
    {
        if (Status != SalesOrderStatus.Approved)
            throw new DomainRuleException("只有已审核订单可以锁定库存。");
        Status = SalesOrderStatus.Reserved;
    }

    public void Cancel()
    {
        if (Status is SalesOrderStatus.PartiallyShipped or SalesOrderStatus.Completed)
            throw new DomainRuleException("已发货订单不能直接取消，应通过退货或红冲处理。");
        Status = SalesOrderStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != SalesOrderStatus.Draft)
            throw new DomainRuleException("只有草稿销售单可以修改。");
    }
}

