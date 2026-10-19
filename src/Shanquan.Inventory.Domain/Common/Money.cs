using System.Globalization;

namespace Shanquan.Inventory.Domain.Common;

public readonly record struct Money(long Cents) : IComparable<Money>
{
    public static Money Zero => new(0);

    public static Money FromYuan(decimal yuan) =>
        new(checked((long)decimal.Round(yuan * 100m, 0, MidpointRounding.AwayFromZero)));

    public decimal Yuan => Cents / 100m;

    public static Money operator +(Money left, Money right) => new(checked(left.Cents + right.Cents));

    public static Money operator -(Money left, Money right) => new(checked(left.Cents - right.Cents));

    public static Money operator *(Money money, long quantity) => new(checked(money.Cents * quantity));

    public int CompareTo(Money other) => Cents.CompareTo(other.Cents);

    public override string ToString() => Yuan.ToString("C2", CultureInfo.GetCultureInfo("zh-CN"));
}

