using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Responses;

/// <summary>
/// Represents an operation result with no return value.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct Result
{
    private readonly Error? _error;
    private readonly ErrorCollection _errors;

    /// <summary>
    /// Gets the error details when the result represents a failure.
    /// </summary>
    [JsonIgnore]
    public Error Error
    {
        get
        {
            if (IsSuccess)
                throw new InvalidOperationException(ResultMessages.ErrorMessageToSuccess);
            return _error ?? Error.Unknown;
        }
    }

    /// <summary>
    /// Gets a collection of all errors when the result represents a failure.
    /// </summary>
    public ErrorCollection Errors => IsSuccess ? ErrorCollection.Empty : _errors;

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("isSuccessful")]
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    [JsonPropertyName("isFailed")]
    public bool IsFailed => !IsSuccess;

    // Coalesces to a well-defined sentinel so default(Result) surfaces a coherent
    // failure instead of a NullReferenceException from the nullable backing field.
    private Error FailureError => _error ?? Error.Unknown;

    internal Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        _error = error;
        _errors = error.HasValue ? new ErrorCollection(error.Value) : ErrorCollection.Empty;
    }

    internal Result(bool isSuccess, ErrorCollection errors)
    {
        IsSuccess = isSuccess;
        _error = errors.Count > 0 ? Error.FromError(errors[0]) : default;
        _errors = errors;
    }

    /// <inheritdoc />
    public override readonly string ToString() => IsSuccess ? "Result[Success]" : $"Result[Failed: {_error?.Code}]";

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    [DebuggerStepThrough]
    public static Result Ok() => new(isSuccess: true, error: default);

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<T> Ok<T>(T value) => new(isSuccess: true, error: default, value: value);

    /// <summary>
    /// Creates a successful result with typed error.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<TValue, TError> Ok<TValue, TError>(TValue value) where TError : IError => new(isSuccess: true, error: default!, value: value);

    /// <summary>
    /// Creates a failed result with the specified error code and message.
    /// </summary>
    [DebuggerStepThrough]
    public static Result Fail(string code, string message) => new(isSuccess: false, error: new Error(code, message));

    /// <summary>
    /// Creates a failed result from a named tuple.
    /// </summary>
    [DebuggerStepThrough]
    public static Result Fail((string Code, string Message) error) => new(isSuccess: false, error: new Error(error.Code, error.Message));

    /// <summary>
    /// Creates a failed result from a tuple.
    /// </summary>
    [DebuggerStepThrough]
    public static Result Fail(Tuple<string, string> error) => new(isSuccess: false, error: new Error(error.Item1, error.Item2));

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    [DebuggerStepThrough]
    public static Result Fail(Error error) => new(isSuccess: false, error: error);

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    [DebuggerStepThrough]
    public static Result Fail(IEnumerable<IError> errors) => new(isSuccess: false, errors: RequireErrors(errors));

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    [DebuggerStepThrough]
    public static Result Fail(params IError[] errors) => new(isSuccess: false, errors: RequireErrors(errors));

    /// <summary>
    /// Creates a failed result with the specified error code and message.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<T> Fail<T>(string code, string message) => new(isSuccess: false, error: new Error(code, message), value: default!);

    /// <summary>
    /// Creates a failed result from a named tuple.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<T> Fail<T>((string Code, string Message) error) => new(isSuccess: false, error: new Error(error.Code, error.Message), value: default!);

    /// <summary>
    /// Creates a failed result from a tuple.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<T> Fail<T>(Tuple<string, string> error) => new(isSuccess: false, error: new Error(error.Item1, error.Item2), value: default!);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<T> Fail<T>(Error error) => new(isSuccess: false, error: error, value: default!);

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<T> Fail<T>(IEnumerable<IError> errors) => new(isSuccess: false, errors: RequireErrors(errors), value: default!);

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<T> Fail<T>(params IError[] errors) => new(isSuccess: false, errors: RequireErrors(errors), value: default!);

    /// <summary>
    /// Creates a failed result with typed error.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<TValue, TError> Fail<TValue, TError>(TError error) where TError : IError => new(isSuccess: false, error: error, value: default!);

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<TValue, TError> Fail<TValue, TError>(IEnumerable<IError> errors) where TError : IError =>
        new(isSuccess: false, errors: RequireErrors(errors), value: default!);

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<TValue, TError> Fail<TValue, TError>(params IError[] errors) where TError : IError =>
        new(isSuccess: false, errors: RequireErrors(errors), value: default!);

    // A failed result must describe why it failed; an empty error sequence would produce an
    // incoherent failure with no accessible error. Reject it at the factory boundary.
    private static ErrorCollection RequireErrors(IEnumerable<IError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var collection = new ErrorCollection(errors);
        if (collection.Count == 0)
            throw new ArgumentException(ResultMessages.NoErrorsProvided, nameof(errors));
        return collection;
    }

    /// <summary>
    /// Creates a successful result if the condition is true, otherwise a failed result.
    /// </summary>
    [DebuggerStepThrough]
    public static Result OkIf(bool condition, string code, string message) => condition ? Ok() : Fail(code, message);

    /// <summary>
    /// Creates a successful result with value if the condition is true, otherwise a failed result.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<T> OkIf<T>(bool condition, T value, string code, string message) => condition ? Ok(value) : Fail<T>(code, message);

    /// <summary>
    /// Creates a successful result with value if the condition is true, otherwise a failed result with the specified error.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<T> OkIf<T>(bool condition, T value, Error error) => condition ? Ok(value) : Fail<T>(error);

    /// <summary>
    /// Creates a successful result with typed error if the condition is true, otherwise a failed result.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<TValue, TError> OkIf<TValue, TError>(bool condition, TValue value, TError error) where TError : IError => condition ? Ok<TValue, TError>(value) : Fail<TValue, TError>(error);

    /// <summary>
    /// Creates a failed result if the condition is true, otherwise a successful result.
    /// </summary>
    [DebuggerStepThrough]
    public static Result FailIf(bool condition, string code, string message) => !condition ? Ok() : Fail(code, message);

    /// <summary>
    /// Creates a failed result with value if the condition is true, otherwise a successful result.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<T> FailIf<T>(bool condition, T value, string code, string message) => !condition ? Ok(value) : Fail<T>(code, message);

    /// <summary>
    /// Creates a failed result with value and error if the condition is true, otherwise a successful result.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<T> FailIf<T>(bool condition, T value, Error error) => !condition ? Ok(value) : Fail<T>(error);

    /// <summary>
    /// Creates a failed result with typed error if the condition is true, otherwise a successful result.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<TValue, TError> FailIf<TValue, TError>(bool condition, TValue value, TError error) where TError : IError => !condition ? Ok<TValue, TError>(value) : Fail<TValue, TError>(error);

    /// <summary>
    /// Executes the appropriate action based on success or failure state.
    /// </summary>
    public readonly void Match(Action onSuccess, Action<Error> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        if (IsSuccess) onSuccess(); else onFailure(FailureError);
    }

    /// <summary>
    /// Returns the fallback value. Only meaningful on a failed void result — a partial
    /// function that throws on success. Prefer <see cref="Result{T}.Else(T)"/>.
    /// </summary>
    [Obsolete("Else on a void Result throws on success. Use Result<T>.Else instead.")]
    [SuppressMessage("Major Code Smell", "S1133:Deprecated code should be removed",
        Justification = "Intentional public-API deprecation kept for backward compatibility until the next major.")]
    public readonly T Else<T>(T fallbackValue) => IsSuccess ? throw new InvalidOperationException(ResultMessages.ElseOnVoidResult) : fallbackValue;

    /// <summary>
    /// Executes an action without modifying the result if the operation succeeded.
    /// </summary>
    public readonly Result Tap(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsSuccess) action();
        return this;
    }

    /// <summary>
    /// Chains another fallible operation if this result is successful.
    /// </summary>
    public readonly Result Bind(Func<Result> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return IsSuccess ? func() : this;
    }

    /// <summary>
    /// Chains another async fallible operation if this result is successful.
    /// </summary>
    public readonly async Task<Result> BindAsync(Func<Task<Result>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return IsSuccess ? await func().ConfigureAwait(false) : this;
    }

    /// <summary>
    /// Executes an async action without modifying the result if the operation succeeded.
    /// </summary>
    public readonly async Task<Result> TapAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsSuccess) await action().ConfigureAwait(false);
        return this;
    }
}

/// <summary>
/// Represents an operation result with a return value.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct Result<T>
{
    private readonly Error? _error;
    private readonly T? _value;
    private readonly ErrorCollection _errors;

    /// <summary>
    /// Gets the error details when the result represents a failure.
    /// </summary>
    [JsonIgnore]
    public Error Error
    {
        get
        {
            if (IsSuccess)
                throw new InvalidOperationException(ResultMessages.ErrorMessageToSuccess);
            return _error ?? Error.Unknown;
        }
    }

    /// <summary>
    /// Gets a collection of all errors when the result represents a failure.
    /// </summary>
    public ErrorCollection Errors => IsSuccess ? ErrorCollection.Empty : _errors;

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("isSuccessful")]
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    [JsonPropertyName("isFailed")]
    public bool IsFailed => !IsSuccess;

    /// <summary>
    /// Gets the result value when the operation succeeded.
    /// </summary>
    [JsonIgnore]
    public T Value
    {
        get
        {
            if (!IsSuccess)
                throw new InvalidOperationException(ResultMessages.ValueToFailure);
            return _value!;
        }
    }

    /// <summary>
    /// Gets the result value if successful, otherwise returns default(T).
    /// </summary>
    public T? ValueOrDefault => IsSuccess ? _value : default;

    // See Result.FailureError — keeps default(Result<T>) from throwing on error access.
    private Error FailureError => _error ?? Error.Unknown;

    internal Result(bool isSuccess, Error? error, T? value)
    {
        IsSuccess = isSuccess;
        _error = error;
        _value = value;
        _errors = error.HasValue ? new ErrorCollection(error.Value) : ErrorCollection.Empty;
    }

    internal Result(bool isSuccess, ErrorCollection errors, T? value)
    {
        IsSuccess = isSuccess;
        _error = errors.Count > 0 ? Error.FromError(errors[0]) : default;
        _errors = errors;
        _value = value;
    }

    /// <inheritdoc />
    public override readonly string ToString()
    {
        if (IsSuccess) return $"Result<Success: {_value}>";
        return $"Result<Failed: {_error?.Code} - {_error?.Message}>";
    }

    /// <summary>
    /// Transforms the result value using the specified function.
    /// </summary>
    public readonly Result<TOut> Map<TOut>(Func<T, TOut> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return IsSuccess ? Result.Ok(func(_value!)) : new Result<TOut>(false, _errors, default);
    }

    /// <summary>
    /// Chains a fallible operation that may produce a new result.
    /// </summary>
    public readonly Result<TOut> Bind<TOut>(Func<T, Result<TOut>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return IsSuccess ? func(_value!) : new Result<TOut>(false, _errors, default);
    }

    /// <summary>
    /// Executes an action without modifying the result if the operation succeeded.
    /// </summary>
    public readonly Result<T> Tap(Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsSuccess) action(_value!);
        return this;
    }

    /// <summary>
    /// Validates the result value against a predicate, returning failure if the predicate is false.
    /// </summary>
    public readonly Result<T> Ensure(Predicate<T> predicate, Error error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (!IsSuccess) return this;
        if (!predicate(_value!)) return new Result<T>(false, error, default);
        return this;
    }

    /// <summary>
    /// Executes the appropriate function based on success or failure state.
    /// </summary>
    public readonly TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(_value!) : onFailure(FailureError);
    }

    /// <summary>
    /// Executes the appropriate action based on success or failure state.
    /// </summary>
    public readonly void Match(Action<T> onSuccess, Action<Error> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        if (IsSuccess) onSuccess(_value!); else onFailure(FailureError);
    }

    /// <summary>
    /// Returns the result value if successful, otherwise returns the fallback value.
    /// </summary>
    public readonly T Else(T fallbackValue) => IsSuccess ? _value! : fallbackValue;

    /// <summary>
    /// Returns the result value if successful, otherwise returns the result of the fallback function.
    /// </summary>
    public readonly T Else(Func<Error, T> fallbackFunc)
    {
        ArgumentNullException.ThrowIfNull(fallbackFunc);
        return IsSuccess ? _value! : fallbackFunc(FailureError);
    }

    /// <summary>
    /// Transforms the result value using an async function.
    /// </summary>
    public readonly async Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return IsSuccess ? Result.Ok(await func(_value!).ConfigureAwait(false)) : new Result<TOut>(false, _errors, default);
    }

    /// <summary>
    /// Chains a fallible async operation that may produce a new result.
    /// </summary>
    public readonly async Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return IsSuccess ? await func(_value!).ConfigureAwait(false) : new Result<TOut>(false, _errors, default);
    }

    /// <summary>
    /// Executes an async action without modifying the result if the operation succeeded.
    /// </summary>
    public readonly async Task<Result<T>> TapAsync(Func<T, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsSuccess) await action(_value!).ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// Validates the result value against an async predicate.
    /// </summary>
    public readonly async Task<Result<T>> EnsureAsync(Predicate<T> predicate, Error error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (!IsSuccess) return this;
        if (!predicate(_value!)) return new Result<T>(false, error, default);
        return this;
    }

    /// <summary>
    /// Executes the appropriate async function based on success or failure state.
    /// </summary>
    public readonly async Task<TResult> MatchAsync<TResult>(Func<T, Task<TResult>> onSuccess, Func<Error, Task<TResult>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? await onSuccess(_value!).ConfigureAwait(false) : await onFailure(FailureError).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the result value if successful, otherwise returns the result of the async fallback function.
    /// </summary>
    public readonly async Task<T> ElseAsync(Func<Error, Task<T>> fallbackFunc)
    {
        ArgumentNullException.ThrowIfNull(fallbackFunc);
        return IsSuccess ? _value! : await fallbackFunc(FailureError).ConfigureAwait(false);
    }

    /// <summary>
    /// Enables LINQ query syntax for Result. SelectMany is Bind.
    /// </summary>
    public readonly Result<TOut> SelectMany<TOut>(Func<T, Result<TOut>> selector) => Bind(selector);

    /// <summary>
    /// Enables LINQ query syntax with final projection.
    /// </summary>
    public readonly Result<TResult> SelectMany<TIntermediate, TResult>(Func<T, Result<TIntermediate>> collectionSelector, Func<T, TIntermediate, TResult> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(collectionSelector);
        ArgumentNullException.ThrowIfNull(resultSelector);
        if (!IsSuccess) return new Result<TResult>(isSuccess: false, errors: _errors, value: default);

        var intermediate = collectionSelector(_value!);
        if (!intermediate.IsSuccess) return new Result<TResult>(isSuccess: false, errors: intermediate._errors, value: default);

        return new Result<TResult>(isSuccess: true, error: default, value: resultSelector(_value!, intermediate.Value));
    }

    /// <summary>
    /// Enables LINQ select clause.
    /// </summary>
    public readonly Result<TOut> Select<TOut>(Func<T, TOut> selector) => Map(selector);
}

/// <summary>
/// Represents an operation result with a return value and typed error.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct Result<TValue, TError> where TError : IError
{
    private readonly TError? _error;
    private readonly TValue? _value;
    private readonly ErrorCollection _errors;

    /// <summary>
    /// Gets the error details when the result represents a failure.
    /// </summary>
    [JsonIgnore]
    public TError Error
    {
        get
        {
            return IsSuccess
                ? throw new InvalidOperationException(ResultMessages.ErrorMessageToSuccess)
                : _error ?? throw new InvalidOperationException(ResultMessages.DefaultResultHasNoError);
        }
    }

    /// <summary>
    /// Gets a collection of all errors when the result represents a failure.
    /// </summary>
    public ErrorCollection Errors => IsSuccess ? ErrorCollection.Empty : _errors;

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("isSuccessful")]
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    [JsonPropertyName("isFailed")]
    public bool IsFailed => !IsSuccess;

    /// <summary>
    /// Gets the result value when the operation succeeded.
    /// </summary>
    [JsonIgnore]
    public TValue Value
    {
        get => !IsSuccess ? throw new InvalidOperationException(ResultMessages.ValueToFailure) : _value!;
    }

    /// <summary>
    /// Gets the result value if successful, otherwise returns default(TValue).
    /// </summary>
    public TValue? ValueOrDefault => IsSuccess ? _value : default;

    // A generic TError has no sentinel, so a defaulted typed result fails loudly with a
    // clear message rather than handing a null error to a Match/Else callback.
    private TError FailureError => _error ?? throw new InvalidOperationException(ResultMessages.DefaultResultHasNoError);

    internal Result(bool isSuccess, TError? error, TValue? value)
    {
        IsSuccess = isSuccess;
        _error = error;
        _value = value;
        _errors = EqualityComparer<TError?>.Default.Equals(error, default)
            ? ErrorCollection.Empty
            : new ErrorCollection((IError)error!);
    }

    internal Result(bool isSuccess, ErrorCollection errors, TValue? value)
    {
        IsSuccess = isSuccess;
        _error = errors.Count > 0 && errors[0] is TError typed ? typed : default;
        _errors = errors;
        _value = value;
    }

    /// <inheritdoc />
    public override readonly string ToString()
    {
        if (IsSuccess) return $"Result<Success: {_value}>";
        return $"Result<Failed: {_error}>";
    }

    /// <summary>
    /// Transforms the result value using the specified function.
    /// </summary>
    public readonly Result<TOut, TError> Map<TOut>(Func<TValue, TOut> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return IsSuccess ? Result.Ok<TOut, TError>(func(_value!)) : new Result<TOut, TError>(false, _errors, default);
    }

    /// <summary>
    /// Chains a fallible operation that may produce a new result.
    /// </summary>
    public readonly Result<TOut, TError> Bind<TOut>(Func<TValue, Result<TOut, TError>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return IsSuccess ? func(_value!) : new Result<TOut, TError>(false, _errors, default);
    }

    /// <summary>
    /// Executes an action without modifying the result if the operation succeeded.
    /// </summary>
    public readonly Result<TValue, TError> Tap(Action<TValue> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsSuccess) action(_value!);
        return this;
    }

    /// <summary>
    /// Validates the result value against a predicate.
    /// </summary>
    public readonly Result<TValue, TError> Ensure(Predicate<TValue> predicate, TError error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (!IsSuccess) return this;
        if (!predicate(_value!)) return Result.Fail<TValue, TError>(error);
        return this;
    }

    /// <summary>
    /// Executes the appropriate function based on success or failure state.
    /// </summary>
    public readonly TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<TError, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(_value!) : onFailure(FailureError);
    }

    /// <summary>
    /// Executes the appropriate action based on success or failure state.
    /// </summary>
    public readonly void Match(Action<TValue> onSuccess, Action<TError> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        if (IsSuccess) onSuccess(_value!); else onFailure(FailureError);
    }

    /// <summary>
    /// Returns the result value if successful, otherwise returns the fallback value.
    /// </summary>
    public readonly TValue Else(TValue fallbackValue) => IsSuccess ? _value! : fallbackValue;

    /// <summary>
    /// Returns the result value if successful, otherwise returns the result of the fallback function.
    /// </summary>
    public readonly TValue Else(Func<TError, TValue> fallbackFunc)
    {
        ArgumentNullException.ThrowIfNull(fallbackFunc);
        return IsSuccess ? _value! : fallbackFunc(FailureError);
    }

    /// <summary>
    /// Transforms the result value using an async function.
    /// </summary>
    public readonly async Task<Result<TOut, TError>> MapAsync<TOut>(Func<TValue, Task<TOut>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return IsSuccess ? Result.Ok<TOut, TError>(await func(_value!).ConfigureAwait(false)) : new Result<TOut, TError>(false, _errors, default);
    }

    /// <summary>
    /// Chains a fallible async operation.
    /// </summary>
    public readonly async Task<Result<TOut, TError>> BindAsync<TOut>(Func<TValue, Task<Result<TOut, TError>>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return IsSuccess ? await func(_value!).ConfigureAwait(false) : new Result<TOut, TError>(false, _errors, default);
    }

    /// <summary>
    /// Executes an async action without modifying the result.
    /// </summary>
    public readonly async Task<Result<TValue, TError>> TapAsync(Func<TValue, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsSuccess) await action(_value!).ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// Validates the result value against an async predicate.
    /// </summary>
    public readonly async Task<Result<TValue, TError>> EnsureAsync(Predicate<TValue> predicate, TError error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (!IsSuccess) return this;
        if (!predicate(_value!)) return Result.Fail<TValue, TError>(error);
        return this;
    }

    /// <summary>
    /// Executes the appropriate async function based on success or failure state.
    /// </summary>
    public readonly async Task<TResult> MatchAsync<TResult>(Func<TValue, Task<TResult>> onSuccess, Func<TError, Task<TResult>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? await onSuccess(_value!).ConfigureAwait(false) : await onFailure(FailureError).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the result value if successful, otherwise returns the result of the async fallback function.
    /// </summary>
    public readonly async Task<TValue> ElseAsync(Func<TError, Task<TValue>> fallbackFunc)
    {
        ArgumentNullException.ThrowIfNull(fallbackFunc);
        return IsSuccess ? _value! : await fallbackFunc(FailureError).ConfigureAwait(false);
    }

    /// <summary>
    /// Enables LINQ query syntax for Result.
    /// </summary>
    public readonly Result<TOut, TError> SelectMany<TOut>(Func<TValue, Result<TOut, TError>> selector) => Bind(selector);

    /// <summary>
    /// Enables LINQ query syntax with final projection.
    /// </summary>
    public readonly Result<TResult, TError> SelectMany<TIntermediate, TResult>(Func<TValue, Result<TIntermediate, TError>> collectionSelector, Func<TValue, TIntermediate, TResult> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(collectionSelector);
        ArgumentNullException.ThrowIfNull(resultSelector);
        if (!IsSuccess) return new Result<TResult, TError>(false, _errors, default);

        var intermediate = collectionSelector(_value!);
        if (!intermediate.IsSuccess) return new Result<TResult, TError>(false, intermediate._errors, default);

        return Result.Ok<TResult, TError>(resultSelector(_value!, intermediate.Value));
    }

    /// <summary>
    /// Enables LINQ select clause.
    /// </summary>
    public readonly Result<TOut, TError> Select<TOut>(Func<TValue, TOut> selector) => Map(selector);
}

