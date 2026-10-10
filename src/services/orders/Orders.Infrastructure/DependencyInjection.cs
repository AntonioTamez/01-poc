using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Ports.Driven;
using Orders.Infrastructure.Persistence;

namespace Orders.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Name of the connection string (matches the Aspire database resource name).</summary>
    public const string ConnectionStringName = "ordersdb";

    /// <summary>Registers the driven adapters (persistence) of the Orders service.</summary>
    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<OrdersDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());
        });

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<OrdersDbContext>());

        return services;
    }

    /// <summary>
    /// Creates the database schema if it does not exist. Intended for local development and tests only;
    /// production schema management via EF Core migrations is a follow-up.
    /// </summary>
    public static async Task EnsureOrdersDatabaseCreatedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
    }
}
