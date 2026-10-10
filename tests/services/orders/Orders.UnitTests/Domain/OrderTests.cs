using Orders.Domain.Orders;

namespace Orders.UnitTests.Domain;

public sealed class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static Money Eur(decimal amount) => Money.Create(amount, "EUR").Value;

    [Fact]
    public void Place_WithValidLines_CreatesPlacedOrderWithTotal()
    {
        var result = Order.Place(
            CustomerId,
            "EUR",
            [new OrderLineSpec("SKU-1", 2, Eur(10m)), new OrderLineSpec("SKU-2", 1, Eur(5.5m))],
            Now);

        Assert.True(result.IsSuccess);
        var order = result.Value;
        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(CustomerId, order.CustomerId);
        Assert.Equal(Now, order.PlacedAt);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(25.5m, order.Total.Amount);
        Assert.Equal("EUR", order.Total.Currency);
        Assert.NotEqual(Guid.Empty, order.Id.Value);
    }

    [Fact]
    public void Place_WithoutLines_FailsWithNoLines()
    {
        var result = Order.Place(CustomerId, "EUR", [], Now);

        Assert.Equal(OrderErrors.NoLines, result.Error);
    }

    [Fact]
    public void Place_WithEmptyCustomer_FailsWithEmptyCustomer()
    {
        var result = Order.Place(Guid.Empty, "EUR", [new OrderLineSpec("SKU-1", 1, Eur(1m))], Now);

        Assert.Equal(OrderErrors.EmptyCustomer, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Place_WithNonPositiveQuantity_FailsWithInvalidQuantity(int quantity)
    {
        var result = Order.Place(CustomerId, "EUR", [new OrderLineSpec("SKU-1", quantity, Eur(1m))], Now);

        Assert.Equal(OrderErrors.InvalidQuantity, result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Place_WithBlankSku_FailsWithInvalidSku(string sku)
    {
        var result = Order.Place(CustomerId, "EUR", [new OrderLineSpec(sku, 1, Eur(1m))], Now);

        Assert.Equal(OrderErrors.InvalidSku, result.Error);
    }

    [Fact]
    public void Place_WithLineInOtherCurrency_FailsWithCurrencyMismatch()
    {
        var usd = Money.Create(1m, "USD").Value;

        var result = Order.Place(CustomerId, "EUR", [new OrderLineSpec("SKU-1", 1, usd)], Now);

        Assert.Equal(OrderErrors.CurrencyMismatch, result.Error);
    }

    [Fact]
    public void Cancel_Twice_FailsWithAlreadyCancelled()
    {
        var order = Order.Place(CustomerId, "EUR", [new OrderLineSpec("SKU-1", 1, Eur(1m))], Now).Value;

        Assert.True(order.Cancel().IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(OrderErrors.AlreadyCancelled, order.Cancel().Error);
    }
}
