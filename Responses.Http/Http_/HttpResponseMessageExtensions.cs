using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;

namespace Responses.Http;

/// <summary>
/// Flurl extensions for HttpResponseMessage that return Result types with full HTTP metadata.
/// </summary>
public static class HttpResponseMessageExtensions
{
    private const string CancelledCode = "HttpCancelled";
    private const string CancelledMessage = "Request was cancelled";
    private const string NetworkErrorCode = "HttpNetworkError";
    private const string GenericErrorCode = "HttpError";

    /// <summary>
    /// Receives an HTTP response as a <see cref="Result"/> (void success).
    /// </summary>
    public static async Task<Result> ReceiveResult(this Task<HttpResponseMessage> responseTask, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(responseTask);
        try
        {
            using var resp = await responseTask.ConfigureAwait(false);
            return await ToResultAsync(resp, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail(Error.Cancelled(CancelledCode, CancelledMessage));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return Result.Fail(Error.Server(NetworkErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Fail(GenericErrorCode, ex.Message);
        }
    }

    /// <summary>
    /// Receives an IFlurlResponse as a <see cref="Result"/> (void success).
    /// </summary>
    public static async Task<Result> ReceiveResult(this Task<IFlurlResponse> response, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        try
        {
            using var resp = await response.ConfigureAwait(false);
            return await ToResultAsync(resp.ResponseMessage, ct).ConfigureAwait(false);
        }
        catch (FlurlHttpException fex) when (fex.Call?.Response != null)
        {
            return await ToResultAsync(fex.Call.Response.ResponseMessage, ct).ConfigureAwait(false);
        }
        catch (FlurlHttpException fex)
        {
            return Result.Fail(GenericErrorCode, fex.Message);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail(Error.Cancelled(CancelledCode, CancelledMessage));
        }
    }

    /// <summary>
    /// Receives an HTTP response as a <see cref="Result{TValue}"/>.
    /// </summary>
    public static async Task<Result<TValue>> ReceiveResult<TValue>(this Task<HttpResponseMessage> responseTask, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(responseTask);
        try
        {
            using var resp = await responseTask.ConfigureAwait(false);
            return await ToResultAsync<TValue>(resp, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail<TValue>(Error.Cancelled(CancelledCode, CancelledMessage));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return Result.Fail<TValue>(Error.Server(NetworkErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Fail<TValue>(GenericErrorCode, ex.Message);
        }
    }

    /// <summary>
    /// Receives an IFlurlResponse as a <see cref="Result{TValue}"/>.
    /// </summary>
    public static async Task<Result<TValue>> ReceiveResult<TValue>(this Task<IFlurlResponse> response, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        try
        {
            using var resp = await response.ConfigureAwait(false);
            return await ToResultAsync<TValue>(resp.ResponseMessage, ct).ConfigureAwait(false);
        }
        catch (FlurlHttpException fex) when (fex.Call?.Response != null)
        {
            return await ToResultAsync<TValue>(fex.Call.Response.ResponseMessage, ct).ConfigureAwait(false);
        }
        catch (FlurlHttpException fex)
        {
            return Result.Fail<TValue>(GenericErrorCode, fex.Message);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail<TValue>(Error.Cancelled(CancelledCode, CancelledMessage));
        }
    }

    /// <summary>
    /// Receives an HTTP response as a <see cref="Result{TValue,TError}"/> with typed error.
    /// </summary>
    public static async Task<Result<TValue, TError>> ReceiveResult<TValue, TError>(this Task<HttpResponseMessage> responseTask, CancellationToken ct = default)
        where TError : IError
    {
        ArgumentNullException.ThrowIfNull(responseTask);
        try
        {
            using var resp = await responseTask.ConfigureAwait(false);
            return await ToResultAsync<TValue, TError>(resp, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail<TValue, TError>((TError)(IError)Error.Cancelled(CancelledCode, CancelledMessage));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return Result.Fail<TValue, TError>((TError)(IError)Error.Server(NetworkErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Fail<TValue, TError>((TError)(IError)new Error(GenericErrorCode, ex.Message));
        }
    }

    /// <summary>
    /// Receives an IFlurlResponse as a <see cref="Result{TValue,TError}"/> with typed error.
    /// </summary>
    public static async Task<Result<TValue, TError>> ReceiveResult<TValue, TError>(this Task<IFlurlResponse> response, CancellationToken ct = default)
        where TError : IError
    {
        ArgumentNullException.ThrowIfNull(response);
        try
        {
            using var resp = await response.ConfigureAwait(false);
            return await ToResultAsync<TValue, TError>(resp.ResponseMessage, ct).ConfigureAwait(false);
        }
        catch (FlurlHttpException fex) when (fex.Call?.Response != null)
        {
            return await ToResultAsync<TValue, TError>(fex.Call.Response.ResponseMessage, ct).ConfigureAwait(false);
        }
        catch (FlurlHttpException fex)
        {
            return Result.Fail<TValue, TError>((TError)(IError)new Error(GenericErrorCode, fex.Message));
        }
        catch (OperationCanceledException)
        {
            return Result.Fail<TValue, TError>((TError)(IError)Error.Cancelled(CancelledCode, CancelledMessage));
        }
    }

    #region Internal helpers

    private static async Task<Result> ToResultAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var rawBody = await ReadBodyOnceAsync(response, ct).ConfigureAwait(false);

        if (IsSuccessStatus(response.StatusCode))
            return Result.Ok();

        return Result.Fail(CreateHttpError(response.StatusCode, rawBody));
    }

    private static async Task<Result<TValue>> ToResultAsync<TValue>(HttpResponseMessage response, CancellationToken ct)
    {
        var rawBody = await ReadBodyOnceAsync(response, ct).ConfigureAwait(false);

        if (IsSuccessStatus(response.StatusCode))
        {
            var value = TryDeserialize<TValue>(rawBody, out var deserialized) ? deserialized : default;
            return Result.Ok(value!);
        }

        return Result.Fail<TValue>(CreateHttpError(response.StatusCode, rawBody));
    }

    private static async Task<Result<TValue, TError>> ToResultAsync<TValue, TError>(HttpResponseMessage response, CancellationToken ct)
        where TError : IError
    {
        var rawBody = await ReadBodyOnceAsync(response, ct).ConfigureAwait(false);

        if (IsSuccessStatus(response.StatusCode))
        {
            var value = TryDeserialize<TValue>(rawBody, out var deserialized) ? deserialized : default;
            return Result.Ok<TValue, TError>(value!);
        }

        return Result.Fail<TValue, TError>((TError)(IError)CreateHttpError(response.StatusCode, rawBody));
    }

    private static bool IsSuccessStatus(HttpStatusCode statusCode) => (int)statusCode / 100 == 2;

    private static async Task<string> ReadBodyOnceAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // An unreadable body (disposed/aborted stream) degrades to a status-only error.
            return string.Empty;
        }
    }

    private static bool TryDeserialize<T>(string json, out T? value)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            value = default;
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T>(json);
            return true;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // A 2xx body that is not valid JSON for T yields the default value by design.
            value = default;
            return false;
        }
    }

    private static Error CreateHttpError(HttpStatusCode statusCode, string rawBody)
    {
        var errorType = StatusCodeMapping.ToErrorType(statusCode);
        var statusCodeText = ((int)statusCode).ToString(CultureInfo.InvariantCulture);
        var problemDetails = ProblemDetails.TryParse(rawBody);
        if (!problemDetails.HasValue)
            return new Error(statusCodeText, FallbackMessage(statusCode, rawBody), errorType);

        var pd = problemDetails.Value;
        var metadata = new Dictionary<string, string>(3);
        if (!string.IsNullOrEmpty(pd.Type)) metadata["problemType"] = pd.Type;
        if (!string.IsNullOrEmpty(pd.Detail)) metadata["detail"] = pd.Detail;
        if (!string.IsNullOrEmpty(pd.Instance)) metadata["instance"] = pd.Instance;

        var code = string.IsNullOrEmpty(pd.Title) ? statusCodeText : pd.Title;
        var message = string.IsNullOrEmpty(pd.Detail) ? FallbackMessage(statusCode, rawBody) : pd.Detail;
        return new Error(code, message, errorType, metadata);
    }

    // Error requires non-empty code/message, so empty bodies fall back to the status line.
    private static string FallbackMessage(HttpStatusCode statusCode, string rawBody) =>
        string.IsNullOrEmpty(rawBody) ? $"HTTP {(int)statusCode} {statusCode}" : rawBody;

    // TaskCanceledException is intentionally absent: it derives from OperationCanceledException,
    // which every caller catches before reaching this filter.
    private static bool IsNetworkError(Exception ex) =>
        ex is HttpRequestException or IOException;

    #endregion
}

/// <summary>
/// Extensions to access HTTP metadata from a Result.
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>
    /// Returns a tuple of the result and HTTP info. Use with <see cref="HttpResponseMessageExtensions"/>.
    /// For now returns default HttpResponseInfo — full integration requires storing HttpResponseInfo on Result.
    /// </summary>
    public static (Result<TValue> Result, HttpResponseInfo HttpInfo) WithHttpInfo<TValue>(this Result<TValue> result) =>
        (result, default);
}
