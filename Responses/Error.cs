using System;
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
    private static readonly IReadOnlyDictionary<string, string> _emptyMetadata = new Dictionary<string, string>();

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
    /// </summary>
    public static Error Cancelled(string code, string message, IReadOnlyDictionary<string, string>? metadata = null) =>
        new(code, message, ErrorType.Cancelled, metadata);

    private static void ValidateCtor(string code, string message)
    {
        if (string.IsNullOrEmpty(code))
            throw new ArgumentNullException(nameof(code));

        if (string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));
    }

    /// <inheritdoc />
    public override string ToString() => $"[{Type}] [{Layer}] {ApplicationName} - {Code}: {Message}";
}
