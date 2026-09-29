using Credito.Domain;
using Xunit;

namespace Credito.Tests;

public sealed class MoneyTests
{
    [Fact]
    public void Create_AcceptsValidAmount()
    {
        var money = Money.Create(1234.56m);

        Assert.Equal(1234.56m, money.Value);
    }

    [Fact]
    public void Create_RejectsNegativeAmount()
    {
        Assert.Throws<ArgumentException>(
            () => Money.Create(-1m));
    }

    [Fact]
    public void Create_RejectsMoreThanTwoDecimalPlaces()
    {
        Assert.Throws<ArgumentException>(
            () => Money.Create(10.123m));
    }

    [Fact]
    public void SameAmount_HasValueEquality()
    {
        var first = Money.Create(1000m);
        var second = Money.Create(1000.00m);

        Assert.Equal(first, second);
    }
}
