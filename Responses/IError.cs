using System.Collections.Generic;

namespace Responses;

/// <summary>
/// Contract for error types used in Result&lt;TValue, TError&gt;.
/// </summary>
public interface IError
{
    /// <summary>
    /// Gets the error code (machine-readable identifier).
    /// </summary>
    string Code { get; }

    /// <summary>
    /// Gets the error message (human-readable description).
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets the error type for categorization and HTTP status mapping.
    /// </summary>
    ErrorType Type { get; }

    /// <summary>
    /// Gets the layer where the error originated.
    /// </summary>
    string Layer { get; }

    /// <summary>
    /// Gets the application name.
    /// </summary>
    string ApplicationName { get; }

    /// <summary>
    /// Gets additional metadata key-value pairs.
    /// </summary>
    IReadOnlyDictionary<string, string> Metadata { get; }
}
