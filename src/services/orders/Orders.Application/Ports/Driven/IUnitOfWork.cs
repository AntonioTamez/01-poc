namespace Orders.Application.Ports.Driven;

/// <summary>Driven port: commits the changes tracked during a use case atomically.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
