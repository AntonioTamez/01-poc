using Microsoft.Extensions.Time.Testing;
using Orders.Application.Features.GetOrderById;
using Orders.Application.Features.PlaceOrder;
using Orders.Domain.Orders;
using Orders.UnitTests.Fakes;

namespace Orders.UnitTests.Application;

public sealed class OrderHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 30, 0, TimeSpan.Zero);

    private readonly InMemoryOrderRepository _repository = new();
    private readonly PlaceOrderHandler _placeOrder;
    private readonly GetOrderByIdHandler _getOrderById;

    public OrderHandlersTests()
    {
        _placeOrder = new PlaceOrderHandler(_repository, _repository, new FakeTimeProvider(Now));
        _getOrderById = new GetOrderByIdHandler(_repository);
    }

    private static PlaceOrderCommand ValidCommand() => new(
        Guid.NewGuid(),
        "usd",
        [new PlaceOrderLine("SKU-1", 3, 2.5m), new PlaceOrderLine("SKU-2", 1, 10m)]);

    [Fact]
    public async Task PlaceOrder_WithValidCommand_PersistsOrderAndReturnsId()
    {
        var result = await _placeOrder.ExecuteAsync(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _repository.SaveChangesCalls);
        var stored = Assert.Single(_repository.Committed);
        Assert.Equal(result.Value, stored.Id.Value);
        Assert.Equal(Now, stored.PlacedAt);
        Assert.Equal(17.5m, stored.Total.Amount);
    }

    [Fact]
    public async Task PlaceOrder_WithInvalidCurrency_ReturnsValidationErrorAndDoesNotPersist()
    {
        var command = ValidCommand() with { Currency = "dollars" };

        var result = await _placeOrder.ExecuteAsync(command, TestContext.Current.CancellationToken);

        Assert.Equal(OrderErrors.InvalidCurrency, result.Error);
        Assert.Equal(0, _repository.SaveChangesCalls);
    }

    [Fact]
    public async Task PlaceOrder_WithTooManyLines_ReturnsValidationError()
    {
        var lines = Enumerable.Range(0, PlaceOrderValidator.MaxLines + 1)
            .Select(i => new PlaceOrderLine($"SKU-{i}", 1, 1m))
            .ToList();
        var command = ValidCommand() with { Lines = lines };

        var result = await _placeOrder.ExecuteAsync(command, TestContext.Current.CancellationToken);

        Assert.Equal(PlaceOrderValidator.TooManyLines, result.Error);
    }

    [Fact]
    public async Task GetOrderById_AfterPlacing_ReturnsOrder()
    {
        var placed = await _placeOrder.ExecuteAsync(ValidCommand(), TestContext.Current.CancellationToken);

        var result = await _getOrderById.ExecuteAsync(placed.Value, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(placed.Value, result.Value.Id);
        Assert.Equal("USD", result.Value.Currency);
        Assert.Equal("Placed", result.Value.Status);
        Assert.Equal(17.5m, result.Value.Total);
        Assert.Equal(2, result.Value.Lines.Count);
    }

    [Fact]
    public async Task GetOrderById_WhenMissing_ReturnsNotFound()
    {
        var result = await _getOrderById.ExecuteAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(BuildingBlocks.Abstractions.ErrorType.NotFound, result.Error.Type);
    }
}
