using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Responses.Serialization;

/// <summary>
/// System.Text.Json converter for <see cref="Result"/> (void success).
/// </summary>
public class ResultJsonConverter : JsonConverter<Result>
{
    /// <inheritdoc />
    public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        bool? isSuccess = null;
        ErrorCollection? errors = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "isSuccessful":
                    isSuccess = reader.GetBoolean();
                    break;
                case "errors":
                    errors = JsonSerializer.Deserialize<ErrorCollection>(ref reader, options);
                    break;
            }
        }

        if (isSuccess == true)
            return Result.Ok();

        if (errors.HasValue && errors.Value.Count > 0)
            return Result.Fail(errors.Value);

        return Result.Fail("Unknown", "Deserialization error");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("isSuccessful", value.IsSuccess);

        if (value.IsFailed && value.Errors.Count > 0)
        {
            writer.WritePropertyName("errors");
            JsonSerializer.Serialize(writer, value.Errors, options);
        }

        writer.WriteEndObject();
    }
}

/// <summary>
/// System.Text.Json converter for <see cref="Result{T}"/>.
/// </summary>
public class ResultOfTJsonConverter<T> : JsonConverter<Result<T>>
{
    /// <inheritdoc />
    public override Result<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        bool? isSuccess = null;
        T? value = default;
        ErrorCollection? errors = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "isSuccessful":
                    isSuccess = reader.GetBoolean();
                    break;
                case "value":
                    value = JsonSerializer.Deserialize<T>(ref reader, options);
                    break;
                case "errors":
                    errors = JsonSerializer.Deserialize<ErrorCollection>(ref reader, options);
                    break;
            }
        }

        if (isSuccess == true)
            return Result.Ok(value!);

        if (errors.HasValue && errors.Value.Count > 0)
            return Result.Fail<T>(errors.Value);

        return Result.Fail<T>("Unknown", "Deserialization error");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Result<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("isSuccessful", value.IsSuccess);

        if (value.IsSuccess)
        {
            writer.WritePropertyName("value");
            JsonSerializer.Serialize(writer, value.Value, options);
        }
        else if (value.Errors.Count > 0)
        {
            writer.WritePropertyName("errors");
            JsonSerializer.Serialize(writer, value.Errors, options);
        }

        writer.WriteEndObject();
    }
}

/// <summary>
/// System.Text.Json converter for <see cref="Result{TValue, TError}"/>.
/// </summary>
public class ResultOfTValueTErrorJsonConverter<TValue, TError> : JsonConverter<Result<TValue, TError>>
    where TError : IError
{
    /// <inheritdoc />
    public override Result<TValue, TError> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        bool? isSuccess = null;
        TValue? value = default;
        ErrorCollection? errors = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "isSuccessful":
                    isSuccess = reader.GetBoolean();
                    break;
                case "value":
                    value = JsonSerializer.Deserialize<TValue>(ref reader, options);
                    break;
                case "errors":
                    errors = JsonSerializer.Deserialize<ErrorCollection>(ref reader, options);
                    break;
            }
        }

        if (isSuccess == true)
            return Result.Ok<TValue, TError>(value!);

        if (errors.HasValue && errors.Value.Count > 0 && errors.Value[0] is TError typedError)
            return Result.Fail<TValue, TError>(errors.Value);

        var fallbackError = new Error("Unknown", "Deserialization error");
        return Result.Fail<TValue, TError>(fallbackError);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Result<TValue, TError> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("isSuccessful", value.IsSuccess);

        if (value.IsSuccess)
        {
            writer.WritePropertyName("value");
            JsonSerializer.Serialize(writer, value.Value, options);
        }
        else if (value.Errors.Count > 0)
        {
            writer.WritePropertyName("errors");
            JsonSerializer.Serialize(writer, value.Errors, options);
        }

        writer.WriteEndObject();
    }
}
