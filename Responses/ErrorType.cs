namespace Responses;

/// <summary>
/// Categorizes the type of error for routing, handling, and HTTP status mapping.
/// Based on RFC 9110 (HTTP Semantics) and RFC 6585 (Additional HTTP Status Codes).
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Unknown or unspecified error type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Validation error — input data failed business or format rules.
    /// Corresponds to HTTP 400 Bad Request.
    /// </summary>
    Validation = 1,

    /// <summary>
    /// Resource not found.
    /// Corresponds to HTTP 404 Not Found.
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// Conflict — request conflicts with current state (e.g., duplicate resource).
    /// Corresponds to HTTP 409 Conflict.
    /// </summary>
    Conflict = 3,

    /// <summary>
    /// Unauthorized — authentication required or credentials invalid.
    /// Corresponds to HTTP 401 Unauthorized.
    /// </summary>
    Unauthorized = 4,

    /// <summary>
    /// Forbidden — authenticated user lacks permission.
    /// Corresponds to HTTP 403 Forbidden.
    /// </summary>
    Forbidden = 5,

    /// <summary>
    /// Internal server error — unexpected condition.
    /// Corresponds to HTTP 500 Internal Server Error.
    /// </summary>
    ServerError = 6,

    /// <summary>
    /// Operation timed out.
    /// Corresponds to HTTP 408 Request Timeout.
    /// </summary>
    Timeout = 7,

    /// <summary>
    /// Operation was cancelled.
    /// </summary>
    Cancelled = 8,

    /// <summary>
    /// Internal infrastructure error (not a business error).
    /// </summary>
    InternalError = 9,

    // --- RFC 6585 — Additional HTTP Status Codes ---

    /// <summary>
    /// Too many requests — rate limit exceeded.
    /// Corresponds to HTTP 429 Too Many Requests (RFC 6585).
    /// </summary>
    TooManyRequests = 10,

    // --- RFC 9110 — HTTP Semantics (5xx Server Errors) ---

    /// <summary>
    /// Bad gateway — invalid response from upstream server.
    /// Corresponds to HTTP 502 Bad Gateway.
    /// </summary>
    BadGateway = 11,

    /// <summary>
    /// Service unavailable — server temporarily unable to handle request.
    /// Corresponds to HTTP 503 Service Unavailable.
    /// </summary>
    ServiceUnavailable = 12,

    /// <summary>
    /// Gateway timeout — upstream server did not respond in time.
    /// Corresponds to HTTP 504 Gateway Timeout.
    /// </summary>
    GatewayTimeout = 13,

    // --- RFC 9110 — HTTP Semantics (4xx Client Errors) ---

    /// <summary>
    /// Unprocessable entity — well-formed request but semantically incorrect.
    /// Corresponds to HTTP 422 Unprocessable Content (RFC 9110).
    /// </summary>
    UnprocessableEntity = 14,

    /// <summary>
    /// Locked — resource is locked.
    /// Corresponds to HTTP 423 Locked (RFC 4918).
    /// </summary>
    Locked = 15,

    /// <summary>
    /// Failed dependency — request failed due to failure of a previous request.
    /// Corresponds to HTTP 424 Failed Dependency (RFC 4918).
    /// </summary>
    FailedDependency = 16,

    /// <summary>
    /// Upgrade required — client should switch to a different protocol.
    /// Corresponds to HTTP 426 Upgrade Required (RFC 7231).
    /// </summary>
    UpgradeRequired = 17,

    /// <summary>
    /// Precondition required — origin server requires the request to be conditional.
    /// Corresponds to HTTP 428 Precondition Required (RFC 6585).
    /// </summary>
    PreconditionRequired = 18,

    /// <summary>
    /// Unavailable for legal reasons — resource access denied due to legal reasons.
    /// Corresponds to HTTP 451 Unavailable For Legal Reasons (RFC 7725).
    /// </summary>
    UnavailableForLegal = 19,
}
