using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Responses.Serialization;

/// <summary>
/// System.Text.Json source generator context for zero-reflection serialization of the
/// Result data-transfer objects. Serialize <see cref="Result"/> values through their
/// <see cref="ResultDto"/> projections (via FromResult/ToResult) so that the output has a
/// stable shape and round-trips; Result structs are not serialized directly.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(ResultDto))]
[JsonSerializable(typeof(ResultDto<int>))]
[JsonSerializable(typeof(ResultDto<string>))]
[JsonSerializable(typeof(ResultDto<bool>))]
[JsonSerializable(typeof(ResultDto<double>))]
[JsonSerializable(typeof(ResultDto<long>))]
[JsonSerializable(typeof(ResultDto<int, Error>))]
[JsonSerializable(typeof(ResultDto<string, Error>))]
[JsonSerializable(typeof(ErrorDto))]
[JsonSerializable(typeof(Error))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class ResultJsonContext : JsonSerializerContext
{
    /// <summary>
    /// Default serialization options backed by this source-generated context. This is the
    /// context's own read-only options instance, so the camelCase naming policy declared in
    /// <see cref="JsonSourceGenerationOptionsAttribute"/> is honored and the shared instance
    /// cannot be mutated.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions => Default.Options;
}
