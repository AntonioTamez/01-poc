using Microsoft.AspNetCore.Http.HttpResults;
using Orders.Application.Features.GetOrderById;
using Orders.Application.Ports.Driving;

namespace Orders.Api.Endpoints;

internal static class GetOrderByIdEndpoint
{
    public static RouteGroupBuilder MapGetOrderById(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", HandleAsync)
            .WithName("GetOrderById")
            .WithSummary("Gets an order by its identifier.");

        return group;
    }

    private static async Task<Results<Ok<OrderResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        IGetOrderById getOrderById,
        CancellationToken cancellationToken)
    {
        var result = await getOrderById.ExecuteAsync(id, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.ToProblem();
    }
}
