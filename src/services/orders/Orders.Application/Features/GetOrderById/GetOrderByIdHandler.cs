using BuildingBlocks.Abstractions;
using Orders.Application.Ports.Driven;
using Orders.Application.Ports.Driving;
using Orders.Domain.Orders;

namespace Orders.Application.Features.GetOrderById;

internal sealed class GetOrderByIdHandler(IOrderRepository repository) : IGetOrderById
{
    public async Task<Result<OrderResponse>> ExecuteAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var id = new OrderId(orderId);
        var order = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return order is null
            ? Result.Failure<OrderResponse>(OrderErrors.NotFound(id))
            : Result.Success(OrderResponse.From(order));
    }
}
