namespace Orders.Application.Features.PlaceOrder;

public sealed record PlaceOrderCommand(Guid CustomerId, string Currency, IReadOnlyList<PlaceOrderLine> Lines);

public sealed record PlaceOrderLine(string Sku, int Quantity, decimal UnitPrice);
