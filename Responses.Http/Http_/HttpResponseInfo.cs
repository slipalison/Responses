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
/// Covers all standard HTTP codes (RFC 9110, RFC 6585, RFC 4918, RFC 7725)
/// with intelligent fallback for unmapped and custom codes.
/// </summary>
public static class StatusCodeMapping
{
    /// <summary>
    /// Maps an HTTP status code to the most appropriate <see cref="ErrorType"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to map.</param>
    /// <returns>The corresponding ErrorType, or Unknown for non-HTTP-range codes.</returns>
    public static ErrorType ToErrorType(System.Net.HttpStatusCode statusCode) =>
        (int)statusCode switch
        {
            // 4xx Client Errors — specific mappings
            400 => ErrorType.Validation,
            401 => ErrorType.Unauthorized,
            402 => ErrorType.Validation,       // Payment Required (reserved, rarely used)
            403 => ErrorType.Forbidden,
            404 => ErrorType.NotFound,
            405 => ErrorType.Validation,       // Method Not Allowed
            406 => ErrorType.Validation,       // Not Acceptable
            407 => ErrorType.Unauthorized,     // Proxy Authentication Required
            408 => ErrorType.Timeout,          // Request Timeout
            409 => ErrorType.Conflict,
            410 => ErrorType.NotFound,         // Gone
            411 => ErrorType.Validation,       // Length Required
            412 => ErrorType.Validation,       // Precondition Failed
            413 => ErrorType.Validation,       // Content Too Large
            414 => ErrorType.Validation,       // URI Too Long
            415 => ErrorType.Validation,       // Unsupported Media Type
            416 => ErrorType.Validation,       // Range Not Satisfiable
            417 => ErrorType.Validation,       // Expectation Failed
            418 => ErrorType.Unknown,          // I'm a teapot (custom/RFC 2324)
            421 => ErrorType.Validation,       // Misdirected Request
            422 => ErrorType.UnprocessableEntity,
            423 => ErrorType.Locked,
            424 => ErrorType.FailedDependency,
            425 => ErrorType.Validation,       // Too Early
            426 => ErrorType.UpgradeRequired,
            428 => ErrorType.PreconditionRequired,
            429 => ErrorType.TooManyRequests,
            431 => ErrorType.Validation,       // Request Header Fields Too Large
            451 => ErrorType.UnavailableForLegal,

            // 4xx Fallback — unmapped client errors → Validation
            >= 400 and < 500 => ErrorType.Validation,

            // 5xx Server Errors — specific mappings
            500 => ErrorType.ServerError,
            501 => ErrorType.ServerError,      // Not Implemented
            502 => ErrorType.BadGateway,
            503 => ErrorType.ServiceUnavailable,
            504 => ErrorType.GatewayTimeout,
            505 => ErrorType.ServerError,      // HTTP Version Not Supported
            506 => ErrorType.ServerError,      // Variant Also Negotiates
            507 => ErrorType.ServerError,      // Insufficient Storage
            508 => ErrorType.ServerError,      // Loop Detected
            510 => ErrorType.ServerError,      // Not Extended
            511 => ErrorType.ServerError,      // Network Authentication Required

            // 5xx Fallback — unmapped server errors → ServerError
            >= 500 and < 600 => ErrorType.ServerError,

            // Everything else (1xx, 2xx, 3xx, 600+) → Unknown
            _ => ErrorType.Unknown,
        };
}
