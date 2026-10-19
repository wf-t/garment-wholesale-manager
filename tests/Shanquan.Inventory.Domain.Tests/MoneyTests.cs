using Shanquan.Inventory.Domain.Common;

namespace Shanquan.Inventory.Domain.Tests;

public sealed class MoneyTests
{
    [Fact]
    public void FromYuan_UsesExactCents()
    {
        var money = Money.FromYuan(92.01m);

        Assert.Equal(9_201, money.Cents);
        Assert.Equal(92.01m, money.Yuan);
    }

    [Fact]
    public void Multiplication_DoesNotUseFloatingPoint()
    {
        var total = Money.FromYuan(91m) * 6;

        Assert.Equal(54_600, total.Cents);
    }
}

