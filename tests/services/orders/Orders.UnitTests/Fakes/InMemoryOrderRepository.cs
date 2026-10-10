using Orders.Application.Ports.Driven;
using Orders.Domain.Orders;

namespace Orders.UnitTests.Fakes;

/// <summary>In-memory fake of the driven persistence ports. Changes become visible only after SaveChanges.</summary>
internal sealed class InMemoryOrderRepository : IOrderRepository, IUnitOfWork
{
    private readonly Dictionary<OrderId, Order> _committed = [];
    private readonly List<Order> _pending = [];

    public int SaveChangesCalls { get; private set; }

    public IReadOnlyCollection<Order> Committed => _committed.Values;

    public Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        Task.FromResult(_committed.GetValueOrDefault(id));

    public void Add(Order order) => _pending.Add(order);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (var order in _pending)
        {
            _committed[order.Id] = order;
        }

        _pending.Clear();
        SaveChangesCalls++;
        return Task.CompletedTask;
    }
}
