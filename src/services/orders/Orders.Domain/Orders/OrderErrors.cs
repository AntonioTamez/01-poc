using BuildingBlocks.Abstractions;

namespace Orders.Domain.Orders;

public static class OrderErrors
{
    public static readonly Error EmptyCustomer =
        Error.Validation("Orders.EmptyCustomer", "The customer identifier is required.");

    public static readonly Error NoLines =
        Error.Validation("Orders.NoLines", "An order must contain at least one line.");

    public static readonly Error InvalidSku =
        Error.Validation("Orders.InvalidSku", "A line SKU is required and must not exceed 64 characters.");

    public static readonly Error InvalidQuantity =
        Error.Validation("Orders.InvalidQuantity", "A line quantity must be greater than zero.");

    public static readonly Error NegativeAmount =
        Error.Validation("Orders.NegativeAmount", "A monetary amount cannot be negative.");

    public static readonly Error InvalidCurrency =
        Error.Validation("Orders.InvalidCurrency", "The currency must be a three-letter ISO 4217 code.");

    public static readonly Error CurrencyMismatch =
        Error.Validation("Orders.CurrencyMismatch", "All lines must use the order currency.");

    public static readonly Error AlreadyCancelled =
        Error.Conflict("Orders.AlreadyCancelled", "The order is already cancelled.");

    public static Error NotFound(OrderId id) =>
        Error.NotFound("Orders.NotFound", $"Order '{id}' was not found.");
}
