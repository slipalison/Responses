using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Responses.Serialization;

/// <summary>
/// System.Text.Json source generator context for zero-reflection serialization of Result types.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Result))]
[JsonSerializable(typeof(Result<int>))]
[JsonSerializable(typeof(Result<string>))]
[JsonSerializable(typeof(Result<bool>))]
[JsonSerializable(typeof(Result<double>))]
[JsonSerializable(typeof(Result<long>))]
[JsonSerializable(typeof(Result<int, Error>))]
[JsonSerializable(typeof(Result<string, Error>))]
[JsonSerializable(typeof(Error))]
[JsonSerializable(typeof(ErrorCollection))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class ResultJsonContext : JsonSerializerContext
{
    // Lazy defers creation past this type's static initialization: field initializers in a
    // partial class have no cross-file order, so touching Default here could observe null.
    private static readonly System.Lazy<JsonSerializerOptions> _defaultOptions = new(CreateDefaultOptions);

    /// <summary>
    /// Default serialization options backed by this source-generated context.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions => _defaultOptions.Value;

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = Default,
        };
        // Freeze the shared instance so no caller can mutate global serialization behavior.
        options.MakeReadOnly();
        return options;
    }
}
