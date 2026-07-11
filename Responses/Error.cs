using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Responses;

/// <summary>
/// Represents an error with code, message, type, and contextual metadata.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct Error : IError
{
    private static readonly IReadOnlyDictionary<string, string> _emptyMetadata = FrozenDictionary<string, string>.Empty;

    /// <summary>
    /// Gets the error code (machine-readable identifier).
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; }

    /// <summary>
    /// Gets the error message (human-readable description).
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; }

    /// <summary>
    /// Gets the error type for categorization and HTTP status mapping.
    /// </summary>
    [JsonPropertyName("type")]
    public ErrorType Type { get; }

    /// <summary>
    /// Gets the layer where the error originated.
    /// </summary>
    [JsonPropertyName("layer")]
    public string Layer { get; }

    /// <summary>
    /// Gets the application name.
    /// </summary>
    [JsonPropertyName("applicationName")]
    public string ApplicationName { get; }

    /// <summary>
    /// Gets additional metadata key-value pairs.
    /// </summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Creates a new error with the specified code and message.
    /// </summary>
    public Error(string code, string message, ErrorType type = ErrorType.Unknown, IReadOnlyDictionary<string, string>? metadata = null)
    {
        ValidateCtor(code, message);
        Code = code;
        Message = message;
        Type = type;
        Layer = ResultContext.Layer;
        ApplicationName = ResultContext.ApplicationName;
        Metadata = metadata ?? _emptyMetadata;
    }

    /// <summary>
    /// Creates a new error from a named tuple.
    /// </summary>
    public Error((string code, string message) error, ErrorType type = ErrorType.Unknown, IReadOnlyDictionary<string, string>? metadata = null)
    {
        ValidateCtor(error.code, error.message);
        Code = error.code;
        Message = error.message;
        Type = type;
        Layer = ResultContext.Layer;
        ApplicationName = ResultContext.ApplicationName;
        Metadata = metadata ?? _emptyMetadata;
    }

    /// <summary>
    /// Creates a default error with contextual metadata.
    /// </summary>
    public Error()
    {
        Code = string.Empty;
        Message = string.Empty;
        Type = ErrorType.Unknown;
        Layer = ResultContext.Layer;
        ApplicationName = ResultContext.ApplicationName;
        Metadata = _emptyMetadata;
    }

    // Copies an existing IError verbatim, preserving its Layer/ApplicationName instead of
    // recomputing them from the current context; used only by FromError.
    private Error(string code, string message, ErrorType type, string layer, string applicationName, IReadOnlyDictionary<string, string> metadata)
    {
        Code = code;
        Message = message;
        Type = type;
        Layer = layer;
        ApplicationName = applicationName;
        Metadata = metadata;
    }

    /// <summary>
    /// Sentinel error surfaced by a defaulted failed result that carries no explicit error.
    /// </summary>
    internal static readonly Error Unknown = new("Unknown", "No error information was provided.", ErrorType.Unknown);

    /// <summary>
    /// Returns <paramref name="error"/> unchanged when it is already an <see cref="Error"/>,
    /// otherwise a field-for-field copy. Lets any <see cref="IError"/> flow into result types
    /// whose error surface is the concrete <see cref="Error"/> struct without an invalid cast.
    /// </summary>
    internal static Error FromError(IError error) =>
        error is Error concrete
            ? concrete
            : new Error(error.Code, error.Message, error.Type, error.Layer, error.ApplicationName, error.Metadata ?? _emptyMetadata);

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    public static Error Validation(string code, string message, IReadOnlyDictionary<string, string>? metadata = null) =>
        new(code, message, ErrorType.Validation, metadata);

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    public static Error NotFound(string code, string message, IReadOnlyDictionary<string, string>? metadata = null) =>
        new(code, message, ErrorType.NotFound, metadata);

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    public static Error Conflict(string code, string message, IReadOnlyDictionary<string, string>? metadata = null) =>
        new(code, message, ErrorType.Conflict, metadata);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    public static Error Unauthorized(string code, string message, IReadOnlyDictionary<string, string>? metadata = null) =>
        new(code, message, ErrorType.Unauthorized, metadata);

    /// <summary>
    /// Creates a forbidden error.
    /// </summary>
    public static Error Forbidden(string code, string message, IReadOnlyDictionary<string, string>? metadata = null) =>
        new(code, message, ErrorType.Forbidden, metadata);

    /// <summary>
    /// Creates a server error.
    /// </summary>
    public static Error Server(string code, string message, IReadOnlyDictionary<string, string>? metadata = null) =>
        new(code, message, ErrorType.ServerError, metadata);

    /// <summary>
    /// Creates a timeout error.
    /// </summary>
    public static Error Timeout(string code, string message, IReadOnlyDictionary<string, string>? metadata = null) =>
        new(code, message, ErrorType.Timeout, metadata);

    /// <summary>
    /// Creates a cancelled error.
    /// Corresponds to HTTP cancellation (no direct status code).
    /// </summary>
    public static Error Cancelled(string code, string message, IReadOnlyDictionary<string, string>? metadata = null) =>
        new(code, message, ErrorType.Cancelled, metadata);

    /// <summary>
    /// Creates a too many requests error.
    /// Corresponds to HTTP 429 Too Many Requests (RFC 6585).
    /// </summary>
    public static Error TooManyRequests(string code, string message, IReadOnlyDictionary<string, string>? metadata = null) =>
        new(code, message, ErrorType.TooManyRequests, metadata);

    /// <summary>
    /// Creates an unprocessable entity error.
    /// Corresponds to HTTP 422 Unprocessable Content (RFC 9110).
    /// </summary>
    public static Error UnprocessableEntity(string code, string message, IReadOnlyDictionary<string, string>? metadata = null) =>
        new(code, message, ErrorType.UnprocessableEntity, metadata);

    /// <summary>
    /// Creates a bad gateway error.
    /// Corresponds to HTTP 502 Bad Gateway.
    /// </summary>
    public static Error BadGateway(string code, string message, IReadOnlyDictionary<string, string>? metadata = null) =>
        new(code, message, ErrorType.BadGateway, metadata);

    /// <summary>
    /// Creates a service unavailable error.
    /// Corresponds to HTTP 503 Service Unavailable.
    /// </summary>
    public static Error ServiceUnavailable(string code, string message, IReadOnlyDictionary<string, string>? metadata = null) =>
        new(code, message, ErrorType.ServiceUnavailable, metadata);

    /// <summary>
    /// Creates a gateway timeout error.
    /// Corresponds to HTTP 504 Gateway Timeout.
    /// </summary>
    public static Error GatewayTimeout(string code, string message, IReadOnlyDictionary<string, string>? metadata = null) =>
        new(code, message, ErrorType.GatewayTimeout, metadata);

    private static void ValidateCtor(string code, string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);
        ArgumentException.ThrowIfNullOrEmpty(message);
    }

    /// <inheritdoc />
    public override string ToString() => $"[{Type}] [{Layer}] {ApplicationName} - {Code}: {Message}";
}
