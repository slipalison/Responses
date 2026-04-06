namespace Responses;

/// <summary>
/// Categorizes the type of error for routing, handling, and HTTP status mapping.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Unknown or unspecified error type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Validation error — input data failed business or format rules.
    /// </summary>
    Validation = 1,

    /// <summary>
    /// Resource not found.
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// Conflict — request conflicts with current state (e.g., duplicate resource).
    /// </summary>
    Conflict = 3,

    /// <summary>
    /// Unauthorized — authentication required or credentials invalid.
    /// </summary>
    Unauthorized = 4,

    /// <summary>
    /// Forbidden — authenticated user lacks permission.
    /// </summary>
    Forbidden = 5,

    /// <summary>
    /// Internal server error — unexpected condition.
    /// </summary>
    ServerError = 6,

    /// <summary>
    /// Operation timed out.
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
}
