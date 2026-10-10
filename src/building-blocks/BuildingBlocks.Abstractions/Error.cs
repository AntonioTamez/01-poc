using System.Diagnostics.CodeAnalysis;

namespace BuildingBlocks.Abstractions;

/// <summary>Categorizes an <see cref="Error"/> so adapters can translate it (for example to HTTP status codes).</summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
}

/// <summary>Describes why an operation failed. Errors are values, not exceptions.</summary>
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "C#-only codebase; 'Error' is the conventional name in the Result pattern.")]
public sealed record Error(string Code, string Description, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
}
