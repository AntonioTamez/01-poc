using BuildingBlocks.Abstractions;
using Orders.Domain.Orders;

namespace Orders.Application.Features.PlaceOrder;

/// <summary>
/// Validates the shape of the command and translates it into domain inputs.
/// Business invariants stay in the <see cref="Order"/> aggregate.
/// </summary>
internal static class PlaceOrderValidator
{
    public const int MaxLines = 100;

    public static readonly Error TooManyLines =
        Error.Validation("PlaceOrder.TooManyLines", $"An order cannot contain more than {MaxLines} lines.");

    public static Result<IReadOnlyCollection<OrderLineSpec>> Validate(PlaceOrderCommand command)
    {
        if (command.Lines is null || command.Lines.Count == 0)
        {
            return Result.Failure<IReadOnlyCollection<OrderLineSpec>>(OrderErrors.NoLines);
        }

        if (command.Lines.Count > MaxLines)
        {
            return Result.Failure<IReadOnlyCollection<OrderLineSpec>>(TooManyLines);
        }

        var specs = new List<OrderLineSpec>(command.Lines.Count);
        foreach (var line in command.Lines)
        {
            var unitPrice = Money.Create(line.UnitPrice, command.Currency);
            if (unitPrice.IsFailure)
            {
                return Result.Failure<IReadOnlyCollection<OrderLineSpec>>(unitPrice.Error);
            }

            specs.Add(new OrderLineSpec(line.Sku, line.Quantity, unitPrice.Value));
        }

        return Result.Success<IReadOnlyCollection<OrderLineSpec>>(specs);
    }
}
