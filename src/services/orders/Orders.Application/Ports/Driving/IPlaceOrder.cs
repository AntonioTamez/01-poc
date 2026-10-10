using BuildingBlocks.Abstractions;
using Orders.Application.Features.PlaceOrder;

namespace Orders.Application.Ports.Driving;

/// <summary>Driving port: places a new order and returns its identifier.</summary>
public interface IPlaceOrder
{
    Task<Result<Guid>> ExecuteAsync(PlaceOrderCommand command, CancellationToken cancellationToken);
}
