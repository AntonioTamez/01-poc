using BuildingBlocks.Abstractions;

namespace Orders.Domain.Orders;

/// <summary>Order aggregate root. Guards its invariants and owns its lines.</summary>
public sealed class Order
{
    public const int MaxSkuLength = 64;

    private readonly List<OrderLine> _lines = [];

    // Required by the persistence adapter.
    private Order()
    {
        Currency = string.Empty;
    }

    private Order(OrderId id, Guid customerId, string currency, DateTimeOffset placedAt)
    {
        Id = id;
        CustomerId = customerId;
        Currency = currency;
        PlacedAt = placedAt;
        Status = OrderStatus.Placed;
    }

    public OrderId Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Currency { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTimeOffset PlacedAt { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public Money Total => _lines.Aggregate(Money.Zero(Currency), (total, line) => total.Add(line.LineTotal));

    public static Result<Order> Place(
        Guid customerId,
        string currency,
        IReadOnlyCollection<OrderLineSpec> lines,
        DateTimeOffset placedAt)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (customerId == Guid.Empty)
        {
            return Result.Failure<Order>(OrderErrors.EmptyCustomer);
        }

        var currencyCheck = Money.Create(0m, currency);
        if (currencyCheck.IsFailure)
        {
            return Result.Failure<Order>(currencyCheck.Error);
        }

        if (lines.Count == 0)
        {
            return Result.Failure<Order>(OrderErrors.NoLines);
        }

        var order = new Order(OrderId.New(), customerId, currencyCheck.Value.Currency, placedAt);

        foreach (var line in lines)
        {
            var added = order.AddLine(line);
            if (added.IsFailure)
            {
                return Result.Failure<Order>(added.Error);
            }
        }

        return Result.Success(order);
    }

    public Result Cancel()
    {
        if (Status == OrderStatus.Cancelled)
        {
            return Result.Failure(OrderErrors.AlreadyCancelled);
        }

        Status = OrderStatus.Cancelled;
        return Result.Success();
    }

    private Result AddLine(OrderLineSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Sku) || spec.Sku.Length > MaxSkuLength)
        {
            return Result.Failure(OrderErrors.InvalidSku);
        }

        if (spec.Quantity <= 0)
        {
            return Result.Failure(OrderErrors.InvalidQuantity);
        }

        if (!string.Equals(spec.UnitPrice.Currency, Currency, StringComparison.Ordinal))
        {
            return Result.Failure(OrderErrors.CurrencyMismatch);
        }

        _lines.Add(new OrderLine(spec.Sku.Trim(), spec.Quantity, spec.UnitPrice));
        return Result.Success();
    }
}
