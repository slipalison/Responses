using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Responses;

/// <summary>
/// Represents an error with code, message, and contextual metadata.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct Error : IError
{
    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the layer where the error originated.
    /// </summary>
    public string Layer { get; }

    /// <summary>
    /// Gets the application name.
    /// </summary>
    public string ApplicationName { get; }

    /// <summary>
    /// Gets additional error details.
    /// </summary>
    public IEnumerable<KeyValuePair<string, string>> Errors { get; }

    /// <summary>
    /// Creates a new error with the specified code and message.
    /// </summary>
    public Error(string code, string message, IEnumerable<KeyValuePair<string, string>>? errors = null)
    {
        ValidateCtor(code, message);

        Code = code;
        Message = message;
        Layer = ResultContext.Layer;
        ApplicationName = ResultContext.ApplicationName;
        Errors = errors ?? Array.Empty<KeyValuePair<string, string>>();
    }

    /// <summary>
    /// Creates a new error from a named tuple.
    /// </summary>
    public Error((string code, string message) error, IEnumerable<KeyValuePair<string, string>>? errors = null)
    {
        ValidateCtor(error.code, error.message);

        Code = error.code;
        Message = error.message;
        Layer = ResultContext.Layer;
        ApplicationName = ResultContext.ApplicationName;
        Errors = errors ?? Array.Empty<KeyValuePair<string, string>>();
    }

    /// <summary>
    /// Creates a default error with contextual metadata.
    /// </summary>
    public Error()
    {
        Code = string.Empty;
        Message = string.Empty;
        Layer = ResultContext.Layer;
        ApplicationName = ResultContext.ApplicationName;
        Errors = Array.Empty<KeyValuePair<string, string>>();
    }

    private static void ValidateCtor(string code, string message)
    {
        if (string.IsNullOrEmpty(code))
            throw new ArgumentNullException(nameof(code));

        if (string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));
    }

    /// <inheritdoc />
    public override string ToString() => $"[{Layer}] {ApplicationName} - {Code}: {Message}";
}
