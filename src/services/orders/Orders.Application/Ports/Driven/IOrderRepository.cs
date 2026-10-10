using Orders.Domain.Orders;

namespace Orders.Application.Ports.Driven;

/// <summary>Driven port: persistence of the <see cref="Order"/> aggregate.</summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken);

    void Add(Order order);
}
