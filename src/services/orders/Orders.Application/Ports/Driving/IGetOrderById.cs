using BuildingBlocks.Abstractions;
using Orders.Application.Features.GetOrderById;

namespace Orders.Application.Ports.Driving;

/// <summary>Driving port: reads a single order.</summary>
public interface IGetOrderById
{
    Task<Result<OrderResponse>> ExecuteAsync(Guid orderId, CancellationToken cancellationToken);
}
