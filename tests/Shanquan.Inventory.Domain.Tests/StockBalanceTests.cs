using Shanquan.Inventory.Domain.Common;
using Shanquan.Inventory.Domain.Inventory;

namespace Shanquan.Inventory.Domain.Tests;

public sealed class StockBalanceTests
{
    [Fact]
    public void Reserve_ReducesAvailableWithoutReducingOnHand()
    {
        var stock = new StockBalance(Guid.NewGuid(), Guid.NewGuid(), onHand: 20);

        stock.Reserve(6);

        Assert.Equal(20, stock.OnHand);
        Assert.Equal(6, stock.Reserved);
        Assert.Equal(14, stock.Available);
    }

    [Fact]
    public void ShipReserved_ReducesOnHandAndReservationTogether()
    {
        var stock = new StockBalance(Guid.NewGuid(), Guid.NewGuid(), onHand: 20, reserved: 6);

        stock.ShipReserved(6);

        Assert.Equal(14, stock.OnHand);
        Assert.Equal(0, stock.Reserved);
        Assert.Equal(14, stock.Available);
    }

    [Fact]
    public void Reserve_RejectsQuantityAboveAvailable()
    {
        var stock = new StockBalance(Guid.NewGuid(), Guid.NewGuid(), onHand: 10, reserved: 4);

        var exception = Assert.Throws<DomainRuleException>(() => stock.Reserve(7));

        Assert.Contains("可用库存不足", exception.Message);
    }
}

