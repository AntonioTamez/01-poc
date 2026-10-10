using Microsoft.AspNetCore.Http.HttpResults;
using Orders.Application.Features.PlaceOrder;
using Orders.Application.Ports.Driving;

namespace Orders.Api.Endpoints;

internal static class PlaceOrderEndpoint
{
    public static RouteGroupBuilder MapPlaceOrder(this RouteGroupBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("PlaceOrder")
            .WithSummary("Places a new order.")
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<Results<CreatedAtRoute<PlaceOrderResponse>, ProblemHttpResult>> HandleAsync(
        PlaceOrderRequest request,
        IPlaceOrder placeOrder,
        CancellationToken cancellationToken)
    {
        var command = new PlaceOrderCommand(
            request.CustomerId,
            request.Currency,
            request.Lines?.Select(line => new PlaceOrderLine(line.Sku, line.Quantity, line.UnitPrice)).ToList() ?? []);

        var result = await placeOrder.ExecuteAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(new PlaceOrderResponse(result.Value), "GetOrderById", new { id = result.Value })
            : result.Error.ToProblem();
    }
}

internal sealed record PlaceOrderRequest(Guid CustomerId, string Currency, IReadOnlyList<PlaceOrderLineRequest>? Lines);

internal sealed record PlaceOrderLineRequest(string Sku, int Quantity, decimal UnitPrice);

internal sealed record PlaceOrderResponse(Guid Id);
