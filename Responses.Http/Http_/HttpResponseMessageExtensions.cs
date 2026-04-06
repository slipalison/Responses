using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
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
    /// <summary>
    /// Receives an HTTP response as a <see cref="Result"/> (void success).
    /// </summary>
    public static async Task<Result> ReceiveResult(this Task<HttpResponseMessage> responseTask, CancellationToken ct = default)
    {
        try
        {
            using var resp = await responseTask.ConfigureAwait(false);
            var rawBody = await ReadBodyOnceAsync(resp, ct).ConfigureAwait(false);

            if ((int)resp.StatusCode / 100 == 2)
                return Result.Ok();

            var error = CreateHttpError(resp.StatusCode, rawBody);
            return Result.Fail(error);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail(Error.Cancelled("HttpCancelled", "Request was cancelled"));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return Result.Fail(Error.Server("HttpNetworkError", ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Fail("HttpError", ex.Message);
        }
    }

    /// <summary>
    /// Receives an IFlurlResponse as a <see cref="Result"/> (void success).
    /// </summary>
    public static async Task<Result> ReceiveResult(this Task<IFlurlResponse> response, CancellationToken ct = default)
    {
        try
        {
            using var resp = await response.ConfigureAwait(false);
            return await Task.FromResult(resp.ResponseMessage).ReceiveResult(ct).ConfigureAwait(false);
        }
        catch (FlurlHttpException fex) when (fex.Call?.Response != null)
        {
            return await Task.FromResult(fex.Call.Response).ReceiveResult(ct).ConfigureAwait(false);
        }
        catch (FlurlHttpException fex)
        {
            return Result.Fail("HttpError", fex.Message);
        }
    }

    /// <summary>
    /// Receives an HTTP response as a <see cref="Result{TValue}"/>.
    /// </summary>
    public static async Task<Result<TValue>> ReceiveResult<TValue>(this Task<HttpResponseMessage> responseTask, CancellationToken ct = default)
    {
        try
        {
            using var resp = await responseTask.ConfigureAwait(false);
            var rawBody = await ReadBodyOnceAsync(resp, ct).ConfigureAwait(false);

            if ((int)resp.StatusCode / 100 == 2)
            {
                var value = TryDeserialize<TValue>(rawBody, out var deserialized) ? deserialized : default;
                return Result.Ok(value!);
            }

            var error = CreateHttpError(resp.StatusCode, rawBody);
            return Result.Fail<TValue>(error);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail<TValue>(Error.Cancelled("HttpCancelled", "Request was cancelled"));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return Result.Fail<TValue>(Error.Server("HttpNetworkError", ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Fail<TValue>("HttpError", ex.Message);
        }
    }

    /// <summary>
    /// Receives an IFlurlResponse as a <see cref="Result{TValue}"/>.
    /// </summary>
    public static async Task<Result<TValue>> ReceiveResult<TValue>(this Task<IFlurlResponse> response, CancellationToken ct = default)
    {
        try
        {
            using var resp = await response.ConfigureAwait(false);
            return await Task.FromResult(resp.ResponseMessage).ReceiveResult<TValue>(ct).ConfigureAwait(false);
        }
        catch (FlurlHttpException fex) when (fex.Call?.Response != null)
        {
            return await Task.FromResult(fex.Call.Response).ReceiveResult<TValue>(ct).ConfigureAwait(false);
        }
        catch (FlurlHttpException fex)
        {
            return Result.Fail<TValue>("HttpError", fex.Message);
        }
    }

    /// <summary>
    /// Receives an HTTP response as a <see cref="Result{TValue,TError}"/> with typed error.
    /// </summary>
    public static async Task<Result<TValue, TError>> ReceiveResult<TValue, TError>(this Task<HttpResponseMessage> responseTask, CancellationToken ct = default)
        where TError : IError
    {
        try
        {
            using var resp = await responseTask.ConfigureAwait(false);
            var rawBody = await ReadBodyOnceAsync(resp, ct).ConfigureAwait(false);

            if ((int)resp.StatusCode / 100 == 2)
            {
                var value = TryDeserialize<TValue>(rawBody, out var deserialized) ? deserialized : default;
                return Result.Ok<TValue, TError>(value!);
            }

            var error = CreateTypedError<TError>(resp.StatusCode, rawBody);
            return Result.Fail<TValue, TError>(error);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail<TValue, TError>((TError)(IError)Error.Cancelled("HttpCancelled", "Request was cancelled"));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return Result.Fail<TValue, TError>((TError)(IError)Error.Server("HttpNetworkError", ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Fail<TValue, TError>((TError)(IError)new Error("HttpError", ex.Message));
        }
    }

    /// <summary>
    /// Receives an IFlurlResponse as a <see cref="Result{TValue,TError}"/> with typed error.
    /// </summary>
    public static async Task<Result<TValue, TError>> ReceiveResult<TValue, TError>(this Task<IFlurlResponse> response, CancellationToken ct = default)
        where TError : IError
    {
        try
        {
            using var resp = await response.ConfigureAwait(false);
            return await Task.FromResult(resp.ResponseMessage).ReceiveResult<TValue, TError>(ct).ConfigureAwait(false);
        }
        catch (FlurlHttpException fex) when (fex.Call?.Response != null)
        {
            return await Task.FromResult(fex.Call.Response).ReceiveResult<TValue, TError>(ct).ConfigureAwait(false);
        }
        catch (FlurlHttpException fex)
        {
            return Result.Fail<TValue, TError>((TError)(IError)new Error("HttpError", fex.Message));
        }
    }

    #region Internal helpers

    private static async Task<string> ReadBodyOnceAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
#if NET10_0_OR_GREATER
            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool TryDeserialize<T>(string json, out T? value)
    {
        try
        {
            value = JsonSerializer.Deserialize<T>(json);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    private static Error CreateHttpError(System.Net.HttpStatusCode statusCode, string rawBody)
    {
        var problemDetails = ProblemDetails.TryParse(rawBody);
        if (problemDetails.HasValue)
        {
            var pd = problemDetails.Value;
            var metadata = new System.Collections.Generic.Dictionary<string, string>();
            if (!string.IsNullOrEmpty(pd.Type)) metadata["problemType"] = pd.Type;
            if (!string.IsNullOrEmpty(pd.Detail)) metadata["detail"] = pd.Detail;
            if (!string.IsNullOrEmpty(pd.Instance)) metadata["instance"] = pd.Instance;

            return new Error(
                pd.Title ?? statusCode.ToString(),
                pd.Detail ?? rawBody,
                StatusCodeMapping.ToErrorType(statusCode),
                metadata);
        }

        return new Error(
            statusCode.ToString(),
            rawBody,
            StatusCodeMapping.ToErrorType(statusCode));
    }

    private static TError CreateTypedError<TError>(System.Net.HttpStatusCode statusCode, string rawBody)
        where TError : IError
    {
        var problemDetails = ProblemDetails.TryParse(rawBody);
        if (problemDetails.HasValue)
        {
            var pd = problemDetails.Value;
            var metadata = new System.Collections.Generic.Dictionary<string, string>();
            if (!string.IsNullOrEmpty(pd.Type)) metadata["problemType"] = pd.Type;
            if (!string.IsNullOrEmpty(pd.Detail)) metadata["detail"] = pd.Detail;
            if (!string.IsNullOrEmpty(pd.Instance)) metadata["instance"] = pd.Instance;

            return (TError)(IError)new Error(
                pd.Title ?? statusCode.ToString(),
                pd.Detail ?? rawBody,
                StatusCodeMapping.ToErrorType(statusCode),
                metadata);
        }

        return (TError)(IError)new Error(
            statusCode.ToString(),
            rawBody,
            StatusCodeMapping.ToErrorType(statusCode));
    }

    private static bool IsNetworkError(Exception ex) =>
        ex is HttpRequestException or IOException or TaskCanceledException;

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
