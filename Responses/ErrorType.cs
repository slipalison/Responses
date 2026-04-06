namespace Responses;

/// <summary>
/// Categorizes the type of error for routing, handling, and HTTP status mapping.
/// Values match their corresponding HTTP status codes where applicable.
/// Based on RFC 9110 (HTTP Semantics), RFC 6585 (Additional HTTP Status Codes),
/// RFC 4918 (WebDAV), and RFC 7725 (Legal Reasons).
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Unknown or unspecified error type.
    /// </summary>
    Unknown = 0,

    // --- 4xx Client Errors ---

    /// <summary>
    /// Bad Request — generic validation or client error.
    /// Corresponds to HTTP 400.
    /// </summary>
    Validation = 400,

    /// <summary>
    /// Unauthorized — authentication required or credentials invalid.
    /// Corresponds to HTTP 401.
    /// </summary>
    Unauthorized = 401,

    /// <summary>
    /// Payment Required — reserved for future use.
    /// Corresponds to HTTP 402.
    /// </summary>
    PaymentRequired = 402,

    /// <summary>
    /// Forbidden — authenticated user lacks permission.
    /// Corresponds to HTTP 403.
    /// </summary>
    Forbidden = 403,

    /// <summary>
    /// Resource not found.
    /// Corresponds to HTTP 404.
    /// </summary>
    NotFound = 404,

    /// <summary>
    /// Conflict — request conflicts with current state.
    /// Corresponds to HTTP 409.
    /// </summary>
    Conflict = 409,

    /// <summary>
    /// Request Timeout — server timed out waiting for request.
    /// Corresponds to HTTP 408.
    /// </summary>
    Timeout = 408,

    /// <summary>
    /// Gone — resource no longer exists.
    /// Corresponds to HTTP 410.
    /// </summary>
    Gone = 410,

    /// <summary>
    /// Too many requests — rate limit exceeded.
    /// Corresponds to HTTP 429 (RFC 6585).
    /// </summary>
    TooManyRequests = 429,

    /// <summary>
    /// Unprocessable entity — well-formed but semantically incorrect.
    /// Corresponds to HTTP 422 (RFC 9110).
    /// </summary>
    UnprocessableEntity = 422,

    /// <summary>
    /// Locked — resource is locked.
    /// Corresponds to HTTP 423 (RFC 4918).
    /// </summary>
    Locked = 423,

    /// <summary>
    /// Failed dependency — previous request failed.
    /// Corresponds to HTTP 424 (RFC 4918).
    /// </summary>
    FailedDependency = 424,

    /// <summary>
    /// Upgrade required — client should switch protocol.
    /// Corresponds to HTTP 426 (RFC 7231).
    /// </summary>
    UpgradeRequired = 426,

    /// <summary>
    /// Precondition required — request must be conditional.
    /// Corresponds to HTTP 428 (RFC 6585).
    /// </summary>
    PreconditionRequired = 428,

    /// <summary>
    /// Unavailable for legal reasons.
    /// Corresponds to HTTP 451 (RFC 7725).
    /// </summary>
    UnavailableForLegal = 451,

    /// <summary>
    /// Client closed request — connection closed by client.
    /// Non-standard (nginx convention).
    /// </summary>
    ClientClosed = 499,

    // --- 5xx Server Errors ---

    /// <summary>
    /// Internal server error — unexpected condition.
    /// Corresponds to HTTP 500.
    /// </summary>
    ServerError = 500,

    /// <summary>
    /// Bad gateway — invalid response from upstream.
    /// Corresponds to HTTP 502.
    /// </summary>
    BadGateway = 502,

    /// <summary>
    /// Service unavailable — server temporarily unable to handle request.
    /// Corresponds to HTTP 503.
    /// </summary>
    ServiceUnavailable = 503,

    /// <summary>
    /// Gateway timeout — upstream server did not respond.
    /// Corresponds to HTTP 504.
    /// </summary>
    GatewayTimeout = 504,

    // --- Non-HTTP Error Types ---

    /// <summary>
    /// Operation was cancelled (e.g., CancellationToken).
    /// No direct HTTP equivalent.
    /// </summary>
    Cancelled = 999,

    /// <summary>
    /// Internal infrastructure error (not a business error).
    /// No direct HTTP equivalent.
    /// </summary>
    InternalError = 998,
}
