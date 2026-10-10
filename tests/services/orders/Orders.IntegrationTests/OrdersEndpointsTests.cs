using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Orders.IntegrationTests;

public sealed class OrdersEndpointsTests(OrdersApiFactory factory) : IClassFixture<OrdersApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PlaceOrder_ThenGetById_ReturnsPersistedOrder()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var customerId = Guid.NewGuid();
        var request = new
        {
            customerId,
            currency = "EUR",
            lines = new[]
            {
                new { sku = "SKU-1", quantity = 2, unitPrice = 10.25m },
                new { sku = "SKU-2", quantity = 1, unitPrice = 4.50m },
            },
        };

        var created = await _client.PostAsJsonAsync(new Uri("/orders", UriKind.Relative), request, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var placed = await created.Content.ReadFromJsonAsync<PlacedOrder>(cancellationToken);
        Assert.NotNull(placed);

        var order = await _client.GetFromJsonAsync<OrderDto>(created.Headers.Location, cancellationToken);

        Assert.NotNull(order);
        Assert.Equal(placed.Id, order.Id);
        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal("Placed", order.Status);
        Assert.Equal("EUR", order.Currency);
        Assert.Equal(25.00m, order.Total);
        Assert.Equal(2, order.Lines.Count);
    }

    [Fact]
    public async Task PlaceOrder_WithoutLines_ReturnsValidationProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new { customerId = Guid.NewGuid(), currency = "EUR", lines = Array.Empty<object>() };

        var response = await _client.PostAsJsonAsync(new Uri("/orders", UriKind.Relative), request, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        Assert.Equal("Orders.NoLines", problem?.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.GetAsync(
            new Uri($"/orders/{Guid.NewGuid()}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record PlacedOrder(Guid Id);

    private sealed record OrderDto(
        Guid Id,
        Guid CustomerId,
        string Status,
        string Currency,
        decimal Total,
        DateTimeOffset PlacedAt,
        IReadOnlyList<OrderLineDto> Lines);

    private sealed record OrderLineDto(string Sku, int Quantity, decimal UnitPrice, decimal LineTotal);
}
