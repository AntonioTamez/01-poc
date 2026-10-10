using BuildingBlocks.Abstractions;
using Orders.Application.Ports.Driven;
using Orders.Application.Ports.Driving;
using Orders.Domain.Orders;

namespace Orders.Application.Features.PlaceOrder;

internal sealed class PlaceOrderHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IPlaceOrder
{
    public async Task<Result<Guid>> ExecuteAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var lines = PlaceOrderValidator.Validate(command);
        if (lines.IsFailure)
        {
            return Result.Failure<Guid>(lines.Error);
        }

        var order = Order.Place(command.CustomerId, command.Currency, lines.Value, timeProvider.GetUtcNow());
        if (order.IsFailure)
        {
            return Result.Failure<Guid>(order.Error);
        }

        repository.Add(order.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(order.Value.Id.Value);
    }
}
