namespace Orders.Domain.Orders;

/// <summary>Strongly typed identifier of an <see cref="Order"/>.</summary>
public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
