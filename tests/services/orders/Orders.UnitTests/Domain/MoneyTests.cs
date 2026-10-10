using Orders.Domain.Orders;

namespace Orders.UnitTests.Domain;

public sealed class MoneyTests
{
    [Fact]
    public void Create_NormalizesCurrencyToUpperCase()
    {
        var money = Money.Create(10m, "eur").Value;

        Assert.Equal("EUR", money.Currency);
        Assert.Equal(10m, money.Amount);
    }

    [Fact]
    public void Create_WithNegativeAmount_FailsWithNegativeAmount()
    {
        Assert.Equal(OrderErrors.NegativeAmount, Money.Create(-0.01m, "EUR").Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    public void Create_WithInvalidCurrency_FailsWithInvalidCurrency(string? currency)
    {
        Assert.Equal(OrderErrors.InvalidCurrency, Money.Create(1m, currency).Error);
    }

    [Fact]
    public void Add_WithDifferentCurrencies_Throws()
    {
        var eur = Money.Create(1m, "EUR").Value;
        var usd = Money.Create(1m, "USD").Value;

        Assert.Throws<InvalidOperationException>(() => eur.Add(usd));
    }
}
