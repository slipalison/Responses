using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Responses;

/// <summary>
/// Represents the result of an operation that may or may not succeed.
/// This is the primary entry point for creating Result instances.
/// </summary>
public static class Result
{
    [DebuggerStepThrough]
    public static Result Ok() => new(true, Error.None);

    [DebuggerStepThrough]
    public static Result Fail(string code, string message) => new(false, new Error(code, message));

    [DebuggerStepThrough]
    public static Result Fail((string code, string message) error) => new(false, new Error(error.code, error.message));

    [DebuggerStepThrough]
    public static Result Fail(Tuple<string, string> error) => new(false, new Error(error.Item1, error.Item2));

    [DebuggerStepThrough]
    public static Result Fail(Error error) => new(false, error);

    [DebuggerStepThrough]
    public static Result<T> Ok<T>(T value) => new(true, Error.None, value);

    [DebuggerStepThrough]
    public static Result<T> Fail<T>(string code, string message) => new(false, new Error(code, message), default!);

    [DebuggerStepThrough]
    public static Result<T> Fail<T>((string code, string message) error) => new(false, new Error(error.code, error.message), default!);

    [DebuggerStepThrough]
    public static Result<T> Fail<T>(Tuple<string, string> error) => new(false, new Error(error.Item1, error.Item2), default!);

    [DebuggerStepThrough]
    public static Result<T> Fail<T>(Error error) => new(false, error, default!);

    [DebuggerStepThrough]
    public static Result<TValue, TError> Ok<TValue, TError>(TValue value) where TError : IError => new(true, default!, value);

    [DebuggerStepThrough]
    public static Result<TValue, TError> Fail<TValue, TError>(TError error) where TError : IError => new(false, error, default!);
}

/// <summary>
/// Represents the result of an operation without a return value.
/// </summary>
public readonly struct Result
{
    [JsonProperty(nameof(_error))]
    private readonly Error _error;

    [JsonIgnore]
    public Error Error
    {
        get
        {
            EnsureSuccess();
            return _error;
        }
    }

    [JsonProperty]
    public bool IsSuccess { get; }

    internal Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    /// <summary>
    /// Ensures the result is successful, otherwise throws an exception.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result represents a failure.</exception>
    public void EnsureSuccess()
    {
        if (!IsSuccess)
            throw new InvalidOperationException(ResultMessages.ErrorMessageToSuccess);
    }
}

/// <summary>
/// Represents the result of an operation with a return value.
/// </summary>
/// <typeparam name="T">The type of the value returned by the operation.</typeparam>
public readonly struct Result<T>
{
    [JsonProperty(nameof(_error))]
    private readonly Error _error;

    [JsonIgnore]
    public Error Error
    {
        get
        {
            EnsureSuccess();
            return _error;
        }
    }

    [JsonProperty]
    public bool IsSuccess { get; }

    [JsonProperty(nameof(_value))]
    private readonly T _value;

    [JsonIgnore]
    public T Value
    {
        get
        {
            EnsureSuccess();
            return _value;
        }
    }

    internal Result(bool isSuccess, Error error, T value)
    {
        IsSuccess = isSuccess;
        _error = error;
        _value = value;
    }

    /// <summary>
    /// Ensures the result is successful, otherwise throws an exception.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result represents a failure.</exception>
    public void EnsureSuccess()
    {
        if (!IsSuccess)
            throw new InvalidOperationException(ResultMessages.ValueToFailure);
    }
}

/// <summary>
/// Represents the result of an operation with a custom error type.
/// </summary>
/// <typeparam name="TValue">The type of the value returned by the operation.</typeparam>
/// <typeparam name="TError">The type of the error, which must implement IError.</typeparam>
public readonly struct Result<TValue, TError> where TError : IError
{
    [JsonProperty(nameof(_error))]
    private readonly TError _error;

    [JsonIgnore]
    public TError Error
    {
        get
        {
            EnsureSuccess();
            return _error;
        }
    }

    [JsonProperty]
    public bool IsSuccess { get; }

    [JsonProperty(nameof(_value))]
    private readonly TValue _value;

    [JsonIgnore]
    public TValue Value
    {
        get
        {
            EnsureSuccess();
            return _value;
        }
    }

    internal Result(bool isSuccess, TError error, TValue value)
    {
        IsSuccess = isSuccess;
        _error = error;
        _value = value;
    }

    /// <summary>
    /// Ensures the result is successful, otherwise throws an exception.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result represents a failure.</exception>
    public void EnsureSuccess()
    {
        if (!IsSuccess)
            throw new InvalidOperationException(ResultMessages.ValueToFailure);
    }
}

