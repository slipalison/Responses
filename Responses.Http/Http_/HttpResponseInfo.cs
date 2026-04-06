using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Responses.Http;

/// <summary>
/// Contains HTTP metadata captured alongside a <see cref="Result"/>.
/// </summary>
public readonly struct HttpResponseInfo
{
    /// <summary>
    /// HTTP status code of the response.
    /// </summary>
    public System.Net.HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Response headers.
    /// </summary>
    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

    /// <summary>
    /// Raw response body as a string.
    /// </summary>
    public string RawBody { get; }

    /// <summary>
    /// HTTP reason phrase.
    /// </summary>
    public string ReasonPhrase { get; }

    /// <summary>
    /// Creates a new <see cref="HttpResponseInfo"/>.
    /// </summary>
    public HttpResponseInfo(System.Net.HttpStatusCode statusCode, string reasonPhrase, IReadOnlyDictionary<string, IEnumerable<string>> headers, string rawBody)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        Headers = headers;
        RawBody = rawBody;
    }
}

/// <summary>
/// RFC 9457 Problem Details for HTTP APIs.
/// </summary>
public readonly struct ProblemDetails
{
    public string Type { get; }
    public string Title { get; }
    public int? Status { get; }
    public string Detail { get; }
    public string Instance { get; }

    public ProblemDetails(string type, string title, int? status, string detail, string instance)
    {
        Type = type;
        Title = title;
        Status = status;
        Detail = detail;
        Instance = instance;
    }

    public static ProblemDetails? TryParse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out _))
                return null;

            var type = root.TryGetProperty("type", out var t) ? t.GetString() ?? string.Empty : string.Empty;
            var title = root.TryGetProperty("title", out var ti) ? ti.GetString() ?? string.Empty : string.Empty;
            var status = root.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt32() : (int?)null;
            var detail = root.TryGetProperty("detail", out var d) ? d.GetString() ?? string.Empty : string.Empty;
            var instance = root.TryGetProperty("instance", out var i) ? i.GetString() ?? string.Empty : string.Empty;

            return new ProblemDetails(type, title, status, detail, instance);
        }
        catch
        {
            return null;
        }
    }
}

public static class StatusCodeMapping
{
    public static ErrorType ToErrorType(System.Net.HttpStatusCode statusCode) =>
        (int)statusCode switch
        {
            400 => ErrorType.Validation,
            401 => ErrorType.Unauthorized,
            403 => ErrorType.Forbidden,
            404 => ErrorType.NotFound,
            409 => ErrorType.Conflict,
            >= 500 => ErrorType.ServerError,
            >= 400 => ErrorType.Validation,
            _ => ErrorType.Unknown,
        };
}
