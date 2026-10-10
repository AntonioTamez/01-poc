using BuildingBlocks.Abstractions;

namespace Orders.Domain.Orders;

/// <summary>Non-negative monetary amount in an ISO 4217 currency.</summary>
public sealed record Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string? currency)
    {
        if (amount < 0)
        {
            return Result.Failure<Money>(OrderErrors.NegativeAmount);
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3 || !currency.All(char.IsAsciiLetter))
        {
            return Result.Failure<Money>(OrderErrors.InvalidCurrency);
        }

        return Result.Success(new Money(amount, currency.ToUpperInvariant()));
    }

    public static Money Zero(string currency) => new(0m, currency);

    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cannot add amounts in different currencies.");
        }

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int factor) => new(Amount * factor, Currency);
}
