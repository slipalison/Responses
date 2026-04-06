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

/// <summary>
/// Maps HTTP status codes to <see cref="ErrorType"/> values.
/// Since ErrorType values match HTTP status codes where applicable,
/// this method casts the status code directly and provides intelligent
/// fallback for unmapped or custom codes.
/// </summary>
public static class StatusCodeMapping
{
    /// <summary>
    /// Maps an HTTP status code to the most appropriate <see cref="ErrorType"/>.
    /// For standard HTTP codes, the ErrorType value equals the status code.
    /// For unmapped codes, intelligent fallback is applied.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to map.</param>
    /// <returns>The corresponding ErrorType, or Unknown for non-error codes.</returns>
    public static ErrorType ToErrorType(System.Net.HttpStatusCode statusCode) =>
        (int)statusCode switch
        {
            // Exact matches for defined ErrorType values (value = status code)
            400 => ErrorType.Validation,
            401 => ErrorType.Unauthorized,
            403 => ErrorType.Forbidden,
            404 => ErrorType.NotFound,
            408 => ErrorType.Timeout,
            409 => ErrorType.Conflict,
            410 => ErrorType.Gone,
            422 => ErrorType.UnprocessableEntity,
            423 => ErrorType.Locked,
            424 => ErrorType.FailedDependency,
            426 => ErrorType.UpgradeRequired,
            428 => ErrorType.PreconditionRequired,
            429 => ErrorType.TooManyRequests,
            451 => ErrorType.UnavailableForLegal,
            499 => ErrorType.ClientClosed,
            500 => ErrorType.ServerError,
            502 => ErrorType.BadGateway,
            503 => ErrorType.ServiceUnavailable,
            504 => ErrorType.GatewayTimeout,

            // 4xx Fallback — unmapped client errors → Validation (400)
            >= 400 and < 500 => ErrorType.Validation,

            // 5xx Fallback — unmapped server errors → ServerError (500)
            >= 500 and < 600 => ErrorType.ServerError,

            // 1xx, 2xx, 3xx, 600+ are not errors → Unknown
            _ => ErrorType.Unknown,
        };
}
