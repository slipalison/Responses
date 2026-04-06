using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Responses.Serialization;

/// <summary>
/// Data Transfer Object for <see cref="Result"/> serialization and deserialization.
/// </summary>
public readonly struct ResultDto
{
    public bool IsSuccessful { get; }
    public ErrorDto[] Errors { get; }

    [JsonConstructor]
    public ResultDto(bool isSuccessful, ErrorDto[] errors)
    {
        IsSuccessful = isSuccessful;
        Errors = errors ?? Array.Empty<ErrorDto>();
    }

    public static ResultDto FromResult(Result result)
    {
        var errors = new ErrorDto[result.Errors.Count];
        for (int i = 0; i < result.Errors.Count; i++)
            errors[i] = ErrorDto.FromError(result.Errors[i]);
        return new ResultDto(result.IsSuccess, errors);
    }

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
    public bool IsSuccessful { get; }
    public T? Value { get; }
    public ErrorDto[] Errors { get; }

    [JsonConstructor]
    public ResultDto(bool isSuccessful, T? value, ErrorDto[] errors)
    {
        IsSuccessful = isSuccessful;
        Value = value;
        Errors = errors ?? Array.Empty<ErrorDto>();
    }

    public static ResultDto<T> FromResult(Result<T> result)
    {
        var errors = new ErrorDto[result.Errors.Count];
        for (int i = 0; i < result.Errors.Count; i++)
            errors[i] = ErrorDto.FromError(result.Errors[i]);
        return new ResultDto<T>(result.IsSuccess, result.ValueOrDefault, errors);
    }

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
    public bool IsSuccessful { get; }
    public TValue? Value { get; }
    public ErrorDto[] Errors { get; }

    [JsonConstructor]
    public ResultDto(bool isSuccessful, TValue? value, ErrorDto[] errors)
    {
        IsSuccessful = isSuccessful;
        Value = value;
        Errors = errors ?? Array.Empty<ErrorDto>();
    }

    public static ResultDto<TValue, TError> FromResult(Result<TValue, TError> result)
    {
        var errors = new ErrorDto[result.Errors.Count];
        for (int i = 0; i < result.Errors.Count; i++)
            errors[i] = ErrorDto.FromError(result.Errors[i]);
        return new ResultDto<TValue, TError>(result.IsSuccess, result.ValueOrDefault, errors);
    }

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
    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }
    public string Layer { get; }
    public string ApplicationName { get; }
    public Dictionary<string, string> Metadata { get; }

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

    public static ErrorDto FromError(IError error)
    {
        var metadata = new Dictionary<string, string>();
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

    public Error ToError() => new(Code, Message, Type, Metadata);
}
