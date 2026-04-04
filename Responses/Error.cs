using System;
using System.Collections.Generic;

namespace Responses;

public readonly struct Error : IError
{
    public string Code { get; }

    public string Message { get; }

    public string Layer { get; }

    public string ApplicationName { get; }

    public IEnumerable<KeyValuePair<string, string>> Errors { get; }

    /// <summary>
    /// Represents a none/error-free state.
    /// </summary>
    public static Error None => default;

    public Error(string code, string message, IEnumerable<KeyValuePair<string, string>> errors = null)
    {
        ValidateCtor(code, message);

        Code = code;
        Message = message;
        Layer = ResultContext.Layer;
        ApplicationName = ResultContext.ApplicationName;
        Errors = errors;
    }

    public Error((string code, string message) error, IEnumerable<KeyValuePair<string, string>> errors = null)
    {
        ValidateCtor(error.code, error.message);

        Code = error.code;
        Message = error.message;
        Layer = ResultContext.Layer;
        ApplicationName = ResultContext.ApplicationName;
        Errors = errors;
    }

    private static void ValidateCtor(string code, string message)
    {
        if (string.IsNullOrEmpty(code))
            throw new ArgumentNullException(nameof(code));

        if (string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));
    }

    public override string ToString() => IsNone ? "None" : $"[{Layer}] {ApplicationName} - {Code}: {Message}";

    /// <summary>
    /// Indicates whether this error represents a none/error-free state.
    /// </summary>
    public bool IsNone => string.IsNullOrEmpty(Code) && string.IsNullOrEmpty(Message);
}
