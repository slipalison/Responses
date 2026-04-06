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
    /// <summary>
    /// Default serialization options with Error converter registered.
    /// </summary>
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        TypeInfoResolver = Default,
    };
}
