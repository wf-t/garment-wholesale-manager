using Shanquan.Inventory.Domain.Common;

namespace Shanquan.Inventory.Domain.Inventory;

public sealed class StockBalance
{
    public StockBalance(Guid warehouseId, Guid skuId, long onHand = 0, long reserved = 0)
    {
        if (warehouseId == Guid.Empty) throw new ArgumentException("仓库不能为空。", nameof(warehouseId));
        if (skuId == Guid.Empty) throw new ArgumentException("SKU不能为空。", nameof(skuId));
        if (onHand < 0 || reserved < 0 || reserved > onHand)
            throw new DomainRuleException("库存初始数量无效。锁定库存不能超过实际库存。");

        WarehouseId = warehouseId;
        SkuId = skuId;
        OnHand = onHand;
        Reserved = reserved;
    }

    public Guid WarehouseId { get; }
    public Guid SkuId { get; }
    public long OnHand { get; private set; }
    public long Reserved { get; private set; }
    public long Available => OnHand - Reserved;

    public void Receive(long quantity)
    {
        EnsurePositive(quantity);
        OnHand = checked(OnHand + quantity);
    }

    public void Reserve(long quantity)
    {
        EnsurePositive(quantity);
        if (quantity > Available)
            throw new DomainRuleException($"可用库存不足。可用 {Available}，需要 {quantity}。");

        Reserved = checked(Reserved + quantity);
    }

    public void Release(long quantity)
    {
        EnsurePositive(quantity);
        if (quantity > Reserved)
            throw new DomainRuleException("释放数量不能超过已锁定库存。");

        Reserved -= quantity;
    }

    public void ShipReserved(long quantity)
    {
        EnsurePositive(quantity);
        if (quantity > Reserved)
            throw new DomainRuleException("发货数量不能超过已锁定库存。");

        Reserved -= quantity;
        OnHand -= quantity;
    }

    private static void EnsurePositive(long quantity)
    {
        if (quantity <= 0) throw new DomainRuleException("数量必须大于零。");
    }
}

