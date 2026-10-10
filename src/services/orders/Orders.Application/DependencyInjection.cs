using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Application.Features.GetOrderById;
using Orders.Application.Features.PlaceOrder;
using Orders.Application.Ports.Driving;

namespace Orders.Application;

public static class DependencyInjection
{
    /// <summary>Registers the use cases (driving ports) of the Orders application layer.</summary>
    public static IServiceCollection AddOrdersApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IPlaceOrder, PlaceOrderHandler>();
        services.AddScoped<IGetOrderById, GetOrderByIdHandler>();

        return services;
    }
}
