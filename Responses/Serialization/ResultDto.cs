using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Responses.Serialization;

/// <summary>
/// Data Transfer Object for <see cref="Result"/> serialization and deserialization.
/// </summary>
public readonly struct ResultDto
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccessful { get; }

    /// <summary>
    /// Gets the serialized errors, empty when successful.
    /// </summary>
    public ErrorDto[] Errors { get; }

    /// <summary>
    /// Creates a new <see cref="ResultDto"/>.
    /// </summary>
    [JsonConstructor]
    public ResultDto(bool isSuccessful, ErrorDto[] errors)
    {
        IsSuccessful = isSuccessful;
        Errors = errors ?? Array.Empty<ErrorDto>();
    }

    /// <summary>
    /// Creates a DTO from a <see cref="Result"/>.
    /// </summary>
    public static ResultDto FromResult(Result result)
    {
        var errors = new ErrorDto[result.Errors.Count];
        for (int i = 0; i < result.Errors.Count; i++)
            errors[i] = ErrorDto.FromError(result.Errors[i]);
        return new ResultDto(result.IsSuccess, errors);
    }

    /// <summary>
    /// Converts this DTO back to a <see cref="Result"/>.
    /// </summary>
    public Result ToResult()
    {
        if (IsSuccessful)
            return Result.Ok();

        if (Errors.Length > 0)
        {
            var errors = new IError[Errors.Length];
            for (int i = 0; i < Errors.Length; i++)
                errors[i] = Errors[i].ToError();
            return Result.Fail(errors);
        }

        return Result.Fail("Unknown", "Deserialization error");
    }
}

/// <summary>
/// Data Transfer Object for <see cref="Result{T}"/> serialization and deserialization.
/// </summary>
public readonly struct ResultDto<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccessful { get; }

    /// <summary>
    /// Gets the result value, default when failed.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the serialized errors, empty when successful.
    /// </summary>
    public ErrorDto[] Errors { get; }

    /// <summary>
    /// Creates a new <see cref="ResultDto{T}"/>.
    /// </summary>
    [JsonConstructor]
    public ResultDto(bool isSuccessful, T? value, ErrorDto[] errors)
    {
        IsSuccessful = isSuccessful;
        Value = value;
        Errors = errors ?? Array.Empty<ErrorDto>();
    }

    /// <summary>
    /// Creates a DTO from a <see cref="Result{T}"/>.
    /// </summary>
    public static ResultDto<T> FromResult(Result<T> result)
    {
        var errors = new ErrorDto[result.Errors.Count];
        for (int i = 0; i < result.Errors.Count; i++)
            errors[i] = ErrorDto.FromError(result.Errors[i]);
        return new ResultDto<T>(result.IsSuccess, result.ValueOrDefault, errors);
    }

    /// <summary>
    /// Converts this DTO back to a <see cref="Result{T}"/>.
    /// </summary>
    public Result<T> ToResult()
    {
        if (IsSuccessful)
            return Result.Ok(Value!);

        if (Errors.Length > 0)
        {
            var errors = new IError[Errors.Length];
            for (int i = 0; i < Errors.Length; i++)
                errors[i] = Errors[i].ToError();
            return Result.Fail<T>(errors);
        }

        return Result.Fail<T>("Unknown", "Deserialization error");
    }
}

/// <summary>
/// Data Transfer Object for <see cref="Result{TValue,TError}"/> serialization and deserialization.
/// </summary>
public readonly struct ResultDto<TValue, TError>
    where TError : IError
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccessful { get; }

    /// <summary>
    /// Gets the result value, default when failed.
    /// </summary>
    public TValue? Value { get; }

    /// <summary>
    /// Gets the serialized errors, empty when successful.
    /// </summary>
    public ErrorDto[] Errors { get; }

    /// <summary>
    /// Creates a new <see cref="ResultDto{TValue,TError}"/>.
    /// </summary>
    [JsonConstructor]
    public ResultDto(bool isSuccessful, TValue? value, ErrorDto[] errors)
    {
        IsSuccessful = isSuccessful;
        Value = value;
        Errors = errors ?? Array.Empty<ErrorDto>();
    }

    /// <summary>
    /// Creates a DTO from a <see cref="Result{TValue,TError}"/>.
    /// </summary>
    public static ResultDto<TValue, TError> FromResult(Result<TValue, TError> result)
    {
        var errors = new ErrorDto[result.Errors.Count];
        for (int i = 0; i < result.Errors.Count; i++)
            errors[i] = ErrorDto.FromError(result.Errors[i]);
        return new ResultDto<TValue, TError>(result.IsSuccess, result.ValueOrDefault, errors);
    }

    /// <summary>
    /// Converts this DTO back to a <see cref="Result{TValue,TError}"/>.
    /// </summary>
    public Result<TValue, TError> ToResult()
    {
        if (IsSuccessful)
            return Result.Ok<TValue, TError>(Value!);

        if (Errors.Length > 0)
        {
            var errors = new IError[Errors.Length];
            for (int i = 0; i < Errors.Length; i++)
                errors[i] = Errors[i].ToError();
            return Result.Fail<TValue, TError>(errors);
        }

        return Result.Fail<TValue, TError>((TError)(IError)new Error("Unknown", "Deserialization error"));
    }
}

/// <summary>
/// Data Transfer Object for <see cref="Error"/> serialization and deserialization.
/// </summary>
public readonly struct ErrorDto
{
    /// <summary>
    /// Gets the error code (machine-readable identifier).
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the error message (human-readable description).
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the error type for categorization and HTTP status mapping.
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>
    /// Gets the layer where the error originated.
    /// </summary>
    public string Layer { get; }

    /// <summary>
    /// Gets the application name.
    /// </summary>
    public string ApplicationName { get; }

    /// <summary>
    /// Gets additional metadata key-value pairs.
    /// </summary>
    public Dictionary<string, string> Metadata { get; }

    /// <summary>
    /// Creates a new <see cref="ErrorDto"/>.
    /// </summary>
    [JsonConstructor]
    public ErrorDto(string code, string message, ErrorType type, string layer, string applicationName, Dictionary<string, string>? metadata)
    {
        Code = code ?? string.Empty;
        Message = message ?? string.Empty;
        Type = type;
        Layer = layer ?? string.Empty;
        ApplicationName = applicationName ?? string.Empty;
        Metadata = metadata ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Creates a DTO from any <see cref="IError"/>.
    /// </summary>
    public static ErrorDto FromError(IError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var metadata = new Dictionary<string, string>(error.Metadata.Count);
        foreach (var kvp in error.Metadata)
            metadata[kvp.Key] = kvp.Value;

        return new ErrorDto(
            error.Code,
            error.Message,
            error.Type,
            error.Layer,
            error.ApplicationName,
            metadata);
    }

    /// <summary>
    /// Converts this DTO back to an <see cref="Error"/>.
    /// </summary>
    public Error ToError() => new(Code, Message, Type, Metadata);
}
