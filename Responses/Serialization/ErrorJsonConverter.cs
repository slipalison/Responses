using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Responses.Serialization;

/// <summary>
/// System.Text.Json converter for <see cref="IError"/> and <see cref="Error"/> types.
/// </summary>
public class ErrorJsonConverter : JsonConverter<IError>
{
    /// <inheritdoc />
    public override IError? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        string? code = null;
        string? message = null;
        var type = ErrorType.Unknown;
        string? layer = null;
        string? applicationName = null;
        Dictionary<string, string>? metadata = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "code":
                    code = reader.GetString();
                    break;
                case "message":
                    message = reader.GetString();
                    break;
                case "type":
                    var typeStr = reader.GetString();
                    if (!string.IsNullOrEmpty(typeStr) && Enum.TryParse<ErrorType>(typeStr, out var parsedType))
                        type = parsedType;
                    break;
                case "layer":
                    layer = reader.GetString();
                    break;
                case "applicationName":
                    applicationName = reader.GetString();
                    break;
                case "metadata":
                    metadata = ReadMetadata(ref reader);
                    break;
            }
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(message))
            return null;

        return new Error(code, message, type, metadata);
    }

    private static Dictionary<string, string>? ReadMetadata(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var metadata = new Dictionary<string, string>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var key = reader.GetString()!;
                reader.Read();
                metadata[key] = reader.GetString() ?? string.Empty;
            }
        }
        return metadata.Count > 0 ? metadata : null;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IError value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("code", value.Code);
        writer.WriteString("message", value.Message);
        writer.WriteString("type", value.Type.ToString());
        writer.WriteString("layer", value.Layer);
        writer.WriteString("applicationName", value.ApplicationName);

        if (value.Metadata.Count > 0)
        {
            writer.WriteStartObject("metadata");
            foreach (var kvp in value.Metadata)
                writer.WriteString(kvp.Key, kvp.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}

/// <summary>
/// System.Text.Json converter for <see cref="ErrorCollection"/>.
/// </summary>
public class ErrorCollectionJsonConverter : JsonConverter<ErrorCollection>
{
    /// <inheritdoc />
    public override ErrorCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return ErrorCollection.Empty;

        var errors = new List<IError>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            var error = JsonSerializer.Deserialize<IError>(ref reader, options);
            if (error != null)
                errors.Add(error);
        }

        return new ErrorCollection(errors);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ErrorCollection value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var error in value)
            JsonSerializer.Serialize(writer, error, options);
        writer.WriteEndArray();
    }
}
