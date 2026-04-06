using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
    public Error Error
    {
        get
        {
            if (IsSuccess)
                throw new InvalidOperationException(ResultMessages.ErrorMessageToSuccess);
            return _error!.Value;
        }
    }

    /// <summary>
    /// Gets a collection of all errors when the result represents a failure.
    /// </summary>
    public ErrorCollection Errors => IsSuccess ? ErrorCollection.Empty : _errors;

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailed => !IsSuccess;

    internal Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        _error = error;
        _errors = error.HasValue ? new ErrorCollection(error.Value) : ErrorCollection.Empty;
    }

    internal Result(bool isSuccess, ErrorCollection errors)
    {
        IsSuccess = isSuccess;
        _error = errors.Count > 0 ? (Error)errors[0] : default;
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
    public static Result Fail(IEnumerable<IError> errors) => new(isSuccess: false, errors: new ErrorCollection(errors));

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    [DebuggerStepThrough]
    public static Result Fail(params IError[] errors) => new(isSuccess: false, errors: new ErrorCollection(errors));

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<T> Ok<T>(T value) => new(isSuccess: true, error: default, value: value);

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
    public static Result<T> Fail<T>(IEnumerable<IError> errors) => new(isSuccess: false, errors: new ErrorCollection(errors), value: default!);

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<T> Fail<T>(params IError[] errors) => new(isSuccess: false, errors: new ErrorCollection(errors), value: default!);

    /// <summary>
    /// Creates a successful result with typed error.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<TValue, TError> Ok<TValue, TError>(TValue value) where TError : IError => new(isSuccess: true, error: default!, value: value);

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
        new(isSuccess: false, errors: new ErrorCollection(errors), value: default!);

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    [DebuggerStepThrough]
    public static Result<TValue, TError> Fail<TValue, TError>(params IError[] errors) where TError : IError =>
        new(isSuccess: false, errors: new ErrorCollection(errors), value: default!);

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
        if (IsSuccess) onSuccess(); else onFailure(_error!.Value);
    }

    /// <summary>
    /// Returns the result value if successful, otherwise returns the fallback value.
    /// </summary>
    public readonly T Else<T>(T fallbackValue) => IsSuccess ? throw new InvalidOperationException("Cannot call Else on Result<void>. Use Result<T> instead.") : fallbackValue;

    /// <summary>
    /// Executes an action without modifying the result if the operation succeeded.
    /// </summary>
    public readonly Result Tap(Action action)
    {
        if (IsSuccess) action();
        return this;
    }

    /// <summary>
    /// Chains another fallible operation if this result is successful.
    /// </summary>
    public readonly Result Bind(Func<Result> func) => IsSuccess ? func() : this;

    /// <summary>
    /// Chains another async fallible operation if this result is successful.
    /// </summary>
    public readonly async System.Threading.Tasks.Task<Result> BindAsync(Func<System.Threading.Tasks.Task<Result>> func) => IsSuccess ? await func() : this;

    /// <summary>
    /// Executes an async action without modifying the result if the operation succeeded.
    /// </summary>
    public readonly async System.Threading.Tasks.Task<Result> TapAsync(Func<System.Threading.Tasks.Task> action)
    {
        if (IsSuccess) await action();
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
    public Error Error
    {
        get
        {
            if (IsSuccess)
                throw new InvalidOperationException(ResultMessages.ErrorMessageToSuccess);
            return _error!.Value;
        }
    }

    /// <summary>
    /// Gets a collection of all errors when the result represents a failure.
    /// </summary>
    public ErrorCollection Errors => IsSuccess ? ErrorCollection.Empty : _errors;

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailed => !IsSuccess;

    /// <summary>
    /// Gets the result value when the operation succeeded.
    /// </summary>
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
        _error = errors.Count > 0 ? (Error)errors[0] : default;
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
        return IsSuccess ? Result.Ok(func(_value!)) : new Result<TOut>(false, _error, default);
    }

    /// <summary>
    /// Chains a fallible operation that may produce a new result.
    /// </summary>
    public readonly Result<TOut> Bind<TOut>(Func<T, Result<TOut>> func)
    {
        return IsSuccess ? func(_value!) : new Result<TOut>(false, _error, default);
    }

    /// <summary>
    /// Executes an action without modifying the result if the operation succeeded.
    /// </summary>
    public readonly Result<T> Tap(Action<T> action)
    {
        if (IsSuccess) action(_value!);
        return this;
    }

    /// <summary>
    /// Validates the result value against a predicate, returning failure if the predicate is false.
    /// </summary>
    public readonly Result<T> Ensure(Predicate<T> predicate, Error error)
    {
        if (!IsSuccess) return this;
        if (!predicate(_value!)) return new Result<T>(false, error, default);
        return this;
    }

    /// <summary>
    /// Executes the appropriate function based on success or failure state.
    /// </summary>
    public readonly TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(_value!) : onFailure(_error!.Value);
    }

    /// <summary>
    /// Executes the appropriate action based on success or failure state.
    /// </summary>
    public readonly void Match(Action<T> onSuccess, Action<Error> onFailure)
    {
        if (IsSuccess) onSuccess(_value!); else onFailure(_error!.Value);
    }

    /// <summary>
    /// Returns the result value if successful, otherwise returns the fallback value.
    /// </summary>
    public readonly T Else(T fallbackValue) => IsSuccess ? _value! : fallbackValue;

    /// <summary>
    /// Returns the result value if successful, otherwise returns the result of the fallback function.
    /// </summary>
    public readonly T Else(Func<Error, T> fallbackFunc) => IsSuccess ? _value! : fallbackFunc(_error!.Value);

    /// <summary>
    /// Transforms the result value using an async function.
    /// </summary>
    public readonly async System.Threading.Tasks.Task<Result<TOut>> MapAsync<TOut>(Func<T, System.Threading.Tasks.Task<TOut>> func)
    {
        return IsSuccess ? Result.Ok(await func(_value!)) : new Result<TOut>(false, _error, default);
    }

    /// <summary>
    /// Chains a fallible async operation that may produce a new result.
    /// </summary>
    public readonly async System.Threading.Tasks.Task<Result<TOut>> BindAsync<TOut>(Func<T, System.Threading.Tasks.Task<Result<TOut>>> func)
    {
        return IsSuccess ? await func(_value!) : new Result<TOut>(false, _error, default);
    }

    /// <summary>
    /// Executes an async action without modifying the result if the operation succeeded.
    /// </summary>
    public readonly async System.Threading.Tasks.Task<Result<T>> TapAsync(Func<T, System.Threading.Tasks.Task> action)
    {
        if (IsSuccess) await action(_value!);
        return this;
    }

    /// <summary>
    /// Validates the result value against an async predicate.
    /// </summary>
    public readonly async System.Threading.Tasks.Task<Result<T>> EnsureAsync(Predicate<T> predicate, Error error)
    {
        if (!IsSuccess) return this;
        if (!predicate(_value!)) return new Result<T>(false, error, default);
        return this;
    }

    /// <summary>
    /// Executes the appropriate async function based on success or failure state.
    /// </summary>
    public readonly async System.Threading.Tasks.Task<TResult> MatchAsync<TResult>(Func<T, System.Threading.Tasks.Task<TResult>> onSuccess, Func<Error, System.Threading.Tasks.Task<TResult>> onFailure)
    {
        return IsSuccess ? await onSuccess(_value!) : await onFailure(_error!.Value);
    }

    /// <summary>
    /// Returns the result value if successful, otherwise returns the result of the async fallback function.
    /// </summary>
    public readonly async System.Threading.Tasks.Task<T> ElseAsync(Func<Error, System.Threading.Tasks.Task<T>> fallbackFunc)
    {
        return IsSuccess ? _value! : await fallbackFunc(_error!.Value);
    }

    /// <summary>
    /// Enables LINQ query syntax for Result. SelectMany is Bind.
    /// </summary>
    public readonly Result<TOut> SelectMany<TOut>(Func<T, Result<TOut>> selector) => Bind(selector);

    /// <summary>
    /// Enables LINQ select clause.
    /// </summary>
    public readonly Result<TOut> Select<TOut>(Func<T, TOut> selector) => Map(selector);

    /// <summary>
    /// Enables LINQ query syntax with final projection.
    /// </summary>
    public readonly Result<TResult> SelectMany<TIntermediate, TResult>(Func<T, Result<TIntermediate>> collectionSelector, Func<T, TIntermediate, TResult> resultSelector)
    {
        if (!IsSuccess) return new Result<TResult>(isSuccess: false, error: _error, value: default);

        var intermediate = collectionSelector(_value!);
        if (!intermediate.IsSuccess) return new Result<TResult>(isSuccess: false, error: intermediate._error, value: default);

        return new Result<TResult>(isSuccess: true, error: default, value: resultSelector(_value!, intermediate.Value));
    }
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
    public TError Error
    {
        get
        {
            return !IsSuccess ? _error! : throw new InvalidOperationException(ResultMessages.ErrorMessageToSuccess);
        }
    }

    /// <summary>
    /// Gets a collection of all errors when the result represents a failure.
    /// </summary>
    public ErrorCollection Errors => IsSuccess ? ErrorCollection.Empty : _errors;

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailed => !IsSuccess;

    /// <summary>
    /// Gets the result value when the operation succeeded.
    /// </summary>
    public TValue Value
    {
        get => !IsSuccess ? throw new InvalidOperationException(ResultMessages.ValueToFailure) : _value!;
    }

    /// <summary>
    /// Gets the result value if successful, otherwise returns default(TValue).
    /// </summary>
    public TValue? ValueOrDefault => IsSuccess ? _value : default;

    internal Result(bool isSuccess, TError? error, TValue? value)
    {
        IsSuccess = isSuccess;
        _error = error;
        _value = value;
        _errors = error != null ? new ErrorCollection(error) : ErrorCollection.Empty;
    }

    internal Result(bool isSuccess, ErrorCollection errors, TValue? value)
    {
        IsSuccess = isSuccess;
        _error = errors.Count > 0 ? (TError)errors[0] : default;
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
        return IsSuccess ? Result.Ok<TOut, TError>(func(_value!)) : Result.Fail<TOut, TError>(_error!);
    }

    /// <summary>
    /// Chains a fallible operation that may produce a new result.
    /// </summary>
    public readonly Result<TOut, TError> Bind<TOut>(Func<TValue, Result<TOut, TError>> func)
    {
        return IsSuccess ? func(_value!) : Result.Fail<TOut, TError>(_error!);
    }

    /// <summary>
    /// Executes an action without modifying the result if the operation succeeded.
    /// </summary>
    public readonly Result<TValue, TError> Tap(Action<TValue> action)
    {
        if (IsSuccess) action(_value!);
        return this;
    }

    /// <summary>
    /// Validates the result value against a predicate.
    /// </summary>
    public readonly Result<TValue, TError> Ensure(Predicate<TValue> predicate, TError error)
    {
        if (!IsSuccess) return this;
        if (!predicate(_value!)) return Result.Fail<TValue, TError>(error);
        return this;
    }

    /// <summary>
    /// Executes the appropriate function based on success or failure state.
    /// </summary>
    public readonly TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<TError, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(_value!) : onFailure(_error!);
    }

    /// <summary>
    /// Executes the appropriate action based on success or failure state.
    /// </summary>
    public readonly void Match(Action<TValue> onSuccess, Action<TError> onFailure)
    {
        if (IsSuccess) onSuccess(_value!); else onFailure(_error!);
    }

    /// <summary>
    /// Returns the result value if successful, otherwise returns the fallback value.
    /// </summary>
    public readonly TValue Else(TValue fallbackValue) => IsSuccess ? _value! : fallbackValue;

    /// <summary>
    /// Returns the result value if successful, otherwise returns the result of the fallback function.
    /// </summary>
    public readonly TValue Else(Func<TError, TValue> fallbackFunc) => IsSuccess ? _value! : fallbackFunc(_error!);

    /// <summary>
    /// Transforms the result value using an async function.
    /// </summary>
    public readonly async System.Threading.Tasks.Task<Result<TOut, TError>> MapAsync<TOut>(Func<TValue, System.Threading.Tasks.Task<TOut>> func)
    {
        return IsSuccess ? Result.Ok<TOut, TError>(await func(_value!)) : Result.Fail<TOut, TError>(_error!);
    }

    /// <summary>
    /// Chains a fallible async operation.
    /// </summary>
    public readonly async System.Threading.Tasks.Task<Result<TOut, TError>> BindAsync<TOut>(Func<TValue, System.Threading.Tasks.Task<Result<TOut, TError>>> func)
    {
        return IsSuccess ? await func(_value!) : Result.Fail<TOut, TError>(_error!);
    }

    /// <summary>
    /// Executes an async action without modifying the result.
    /// </summary>
    public readonly async System.Threading.Tasks.Task<Result<TValue, TError>> TapAsync(Func<TValue, System.Threading.Tasks.Task> action)
    {
        if (IsSuccess) await action(_value!);
        return this;
    }

    /// <summary>
    /// Validates the result value against an async predicate.
    /// </summary>
    public readonly async System.Threading.Tasks.Task<Result<TValue, TError>> EnsureAsync(Predicate<TValue> predicate, TError error)
    {
        if (!IsSuccess) return this;
        if (!predicate(_value!)) return Result.Fail<TValue, TError>(error);
        return this;
    }

    /// <summary>
    /// Executes the appropriate async function based on success or failure state.
    /// </summary>
    public readonly async System.Threading.Tasks.Task<TResult> MatchAsync<TResult>(Func<TValue, System.Threading.Tasks.Task<TResult>> onSuccess, Func<TError, System.Threading.Tasks.Task<TResult>> onFailure)
    {
        return IsSuccess ? await onSuccess(_value!) : await onFailure(_error!);
    }

    /// <summary>
    /// Returns the result value if successful, otherwise returns the result of the async fallback function.
    /// </summary>
    public readonly async System.Threading.Tasks.Task<TValue> ElseAsync(Func<TError, System.Threading.Tasks.Task<TValue>> fallbackFunc)
    {
        return IsSuccess ? _value! : await fallbackFunc(_error!);
    }

    /// <summary>
    /// Enables LINQ query syntax for Result.
    /// </summary>
    public readonly Result<TOut, TError> SelectMany<TOut>(Func<TValue, Result<TOut, TError>> selector) => Bind(selector);

    /// <summary>
    /// Enables LINQ select clause.
    /// </summary>
    public readonly Result<TOut, TError> Select<TOut>(Func<TValue, TOut> selector) => Map(selector);

    /// <summary>
    /// Enables LINQ query syntax with final projection.
    /// </summary>
    public readonly Result<TResult, TError> SelectMany<TIntermediate, TResult>(Func<TValue, Result<TIntermediate, TError>> collectionSelector, Func<TValue, TIntermediate, TResult> resultSelector)
    {
        if (!IsSuccess) return Result.Fail<TResult, TError>(_error!);

        var intermediate = collectionSelector(_value!);
        if (!intermediate.IsSuccess) return Result.Fail<TResult, TError>(intermediate.Error);

        return Result.Ok<TResult, TError>(resultSelector(_value!, intermediate.Value));
    }
}

