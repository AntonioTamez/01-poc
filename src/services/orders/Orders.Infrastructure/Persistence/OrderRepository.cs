using Microsoft.EntityFrameworkCore;
using Orders.Application.Ports.Driven;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence;

/// <summary>Driven adapter: implements <see cref="IOrderRepository"/> with EF Core.</summary>
internal sealed class OrderRepository(OrdersDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        dbContext.Orders.FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public void Add(Order order) => dbContext.Orders.Add(order);
}
