using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Flurl.Http;

namespace Responses.Http;

/// <summary>
/// Flurl extensions for HttpResponseMessage that return Result types.
/// </summary>
public static class HttpResponseMessageExtensions
{
    /// <summary>
    /// Receives an HTTP response as a Result (void success).
    /// </summary>
    public static async Task<Result> ReceiveResult(this Task<HttpResponseMessage> responseTask)
    {
        try
        {
            using var resp = await responseTask.ConfigureAwait(false);
            if ((int)resp.StatusCode / 100 == 2)
                return Result.Ok();

            var rawBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            return Result.Fail(resp.StatusCode.ToString(), rawBody);
        }
        catch (Exception ex)
        {
            return Result.Fail("HttpError", ex.Message);
        }
    }

    /// <summary>
    /// Receives an IFlurlResponse as a Result (void success).
    /// </summary>
    public static async Task<Result> ReceiveResult(this Task<IFlurlResponse> response)
    {
        using var resp = await response.ConfigureAwait(false);
        return await Task.FromResult(resp.ResponseMessage).ReceiveResult();
    }

    /// <summary>
    /// Receives an IFlurlResponse as a Result&lt;TValue&gt;.
    /// </summary>
    public static async Task<Result<TValue>> ReceiveResult<TValue>(this Task<IFlurlResponse> response)
    {
        using var resp = await response.ConfigureAwait(false);
        return await Task.FromResult(resp.ResponseMessage).ReceiveResult<TValue>();
    }

    /// <summary>
    /// Receives an HTTP response as a Result&lt;TValue&gt;.
    /// </summary>
    public static async Task<Result<TValue>> ReceiveResult<TValue>(this Task<HttpResponseMessage> responseTask)
    {
        try
        {
            using var resp = await responseTask.ConfigureAwait(false);
            if ((int)resp.StatusCode / 100 == 2)
            {
                var value = await resp.Content.ReadFromJsonAsync<TValue>().ConfigureAwait(false);
                return Result.Ok(value!);
            }

            var rawBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            return Result.Fail<TValue>(resp.StatusCode.ToString(), rawBody);
        }
        catch (Exception ex)
        {
            return Result.Fail<TValue>("HttpError", ex.Message);
        }
    }

    /// <summary>
    /// Receives an HTTP response as a Result&lt;TValue, TError&gt; with typed error.
    /// </summary>
    public static async Task<Result<TValue, TError>> ReceiveResult<TValue, TError>(this Task<HttpResponseMessage> responseTask)
        where TError : IError
    {
        try
        {
            using var resp = await responseTask.ConfigureAwait(false);
            if ((int)resp.StatusCode / 100 == 2)
            {
                var value = await resp.Content.ReadFromJsonAsync<TValue>().ConfigureAwait(false);
                return Result.Ok<TValue, TError>(value!);
            }

            var rawBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var error = (TError)(IError)new Error(resp.StatusCode.ToString(), rawBody);
            return Result.Fail<TValue, TError>(error);
        }
        catch (Exception ex)
        {
            var error = (TError)(IError)new Error("HttpError", ex.Message);
            return Result.Fail<TValue, TError>(error);
        }
    }

    /// <summary>
    /// Receives an IFlurlResponse as a Result&lt;TValue, TError&gt; with typed error.
    /// </summary>
    public static async Task<Result<TValue, TError>> ReceiveResult<TValue, TError>(this Task<IFlurlResponse> response)
        where TError : IError
    {
        using var resp = await response.ConfigureAwait(false);
        return await Task.FromResult(resp.ResponseMessage).ReceiveResult<TValue, TError>();
    }
}
