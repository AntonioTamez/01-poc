using Orders.Domain.Orders;

namespace Orders.Application.Features.GetOrderById;

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    string Currency,
    decimal Total,
    DateTimeOffset PlacedAt,
    IReadOnlyList<OrderLineResponse> Lines)
{
    internal static OrderResponse From(Order order) => new(
        order.Id.Value,
        order.CustomerId,
        order.Status.ToString(),
        order.Currency,
        order.Total.Amount,
        order.PlacedAt,
        [.. order.Lines.Select(line => new OrderLineResponse(line.Sku, line.Quantity, line.UnitPrice.Amount, line.LineTotal.Amount))]);
}

public sealed record OrderLineResponse(string Sku, int Quantity, decimal UnitPrice, decimal LineTotal);
