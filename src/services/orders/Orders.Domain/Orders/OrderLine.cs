namespace Orders.Domain.Orders;

/// <summary>A product line inside an <see cref="Order"/>. Only created through the aggregate.</summary>
public sealed class OrderLine
{
    // Required by the persistence adapter.
    private OrderLine()
    {
        Sku = string.Empty;
        UnitPrice = null!;
    }

    internal OrderLine(string sku, int quantity, Money unitPrice)
    {
        Sku = sku;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public string Sku { get; private set; }

    public int Quantity { get; private set; }

    public Money UnitPrice { get; private set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);
}
