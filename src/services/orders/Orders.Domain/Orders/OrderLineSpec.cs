namespace Orders.Domain.Orders;

/// <summary>Input used to build an <see cref="OrderLine"/> when placing an order.</summary>
public sealed record OrderLineSpec(string Sku, int Quantity, Money UnitPrice);
