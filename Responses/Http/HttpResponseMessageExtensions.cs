using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http;
namespace Responses.Http
{
    public static class HttpResponseMessageExtensions
    {
        private static async Task<(T Response, string ResponseError)> ReadJson<T>(this HttpResponseMessage response,
            JsonSerializer serializer = null)
        {
            if (response.Content == null)
                return (default, null);
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var textReader = new StreamReader(stream);
            using var reader = new JsonTextReader(textReader);
            try
            {
                return ((serializer ?? JsonSerializer.CreateDefault()).Deserialize<T>(reader), null);
            }
            catch (Exception e)
            {
                var j = await response.Content.ReadAsStringAsync();
                return (default, j);
            }
        }
        public static async Task<Result> ReceiveResult(this Task<HttpResponseMessage> response, JsonSerializer serializer = null)
        {
            try
            {
                using var resp = await response.ConfigureAwait(false);
                switch ((int)resp.StatusCode / 100)
                {
                    case 2:
                        return Result.Ok();
                    case 4:
                    case 5:
                        var error = await resp.ReadJson<Error>(serializer);
                        return !string.IsNullOrEmpty(error.ResponseError)
                            ? Result.Fail(resp.StatusCode.ToString(), error.ResponseError)
                            : Result.Fail(error.Response ?? ErrorResolver(resp));
                    default:
                        throw new InvalidOperationException($"Unknown HTTP Status ({resp.StatusCode})");
                }
            }
            catch (JsonSerializationException)
            {
                var result = await response.Result?.Content?.ReadAsStringAsync();
                return Result.Fail(((int)response.Result.StatusCode).ToString(),
                    !string.IsNullOrWhiteSpace(result)
                        ? await response.Result.Content.ReadAsStringAsync()
                        : response.Result.StatusCode.ToString());
            }
            catch (JsonReaderException)
            {
                var result = await response.Result?.Content?.ReadAsStringAsync();
                return Result.Fail(((int)response.Result.StatusCode).ToString(),
                    !string.IsNullOrWhiteSpace(result)
                        ? await response.Result.Content.ReadAsStringAsync()
                        : response.Result.StatusCode.ToString());
            }
            catch (Exception ex)
            {
                return Result.Fail((await response.ConfigureAwait(false))?.StatusCode.ToString(), ex.Message);
            }
        }
        public static async Task<Result> ReceiveResult(this System.Threading.Tasks.Task<Flurl.Http.IFlurlResponse> response,
            JsonSerializer serializer = null)
        {
            using var resp = await response.ConfigureAwait(false);
            return await Task.FromResult(resp.ResponseMessage).ReceiveResult();
        }
        public static async Task<Result<TValue>> ReceiveResult<TValue>(this Task<IFlurlResponse> response,
            JsonSerializer serializer = null)
        {
            using var resp = await response.ConfigureAwait(false);
            return await Task.FromResult(resp.ResponseMessage).ReceiveResult<TValue>();
        }
        public static async Task<Result<TValue>> ReceiveResult<TValue>(this Task<HttpResponseMessage> response,
            JsonSerializer serializer = null)
        {
            try
            {
                using var resp = await response.ConfigureAwait(false);
                switch ((int)resp.StatusCode / 100)
                {
                    case 2:
                        var value = await resp.ReadJson<TValue>(serializer);
                        return Result.Ok(value.Response);
                    case 4:
                    case 5:
                        var error = await resp.ReadJson<Error>(serializer);
                        return !string.IsNullOrEmpty(error.ResponseError)
                            ? Result.Fail<TValue>(resp.StatusCode.ToString(), error.ResponseError)
                            : Result.Fail<TValue>(error.Response ?? ErrorResolver(resp));
                    default:
                        throw new InvalidOperationException($"Unknown HTTP Status ({resp.StatusCode})");
                }
            }
            catch (JsonSerializationException)
            {
                var result = await response.Result?.Content?.ReadAsStringAsync();
                return Result.Fail<TValue>(((int)response.Result.StatusCode).ToString(),
                    !string.IsNullOrWhiteSpace(result)
                        ? await response.Result.Content.ReadAsStringAsync()
                        : response.Result.StatusCode.ToString());
            }
            catch (JsonReaderException)
            {
                var result = await response.Result?.Content?.ReadAsStringAsync();
                return Result.Fail<TValue>(((int)response.Result.StatusCode).ToString(),
                    !string.IsNullOrWhiteSpace(result)
                        ? await response.Result.Content.ReadAsStringAsync()
                        : response.Result.StatusCode.ToString());
            }
            catch (Exception ex)
            {
                return Result.Fail<TValue>((await response.ConfigureAwait(false))?.StatusCode.ToString(), ex.Message);
            }
        }
        public static async Task<Result<TValue, TError>> ReceiveResult<TValue, TError>(this Task<HttpResponseMessage> response,
            JsonSerializer serializer = null)
            where TError : IError
        {
            using var resp = await response.ConfigureAwait(false);
            switch ((int)resp.StatusCode / 100)
            {
                case 2:
                    var value = await resp.ReadJson<TValue>(serializer);
                    return Result.Ok<TValue, TError>(value.Response);
                case 4:
                case 5:
                    var error = await resp.ReadJson<TError>(serializer);
                    return !string.IsNullOrEmpty(error.ResponseError)
                        ? Result.Fail<TValue, TError>((TError)(IError)new Error(resp.StatusCode.ToString(), error.ResponseError))
                        : Result.Fail<TValue, TError>(error.Response ?? ErrorResolver(resp, error.Response));
                default:
                    throw new InvalidOperationException($"Unknown HTTP Status ({resp.StatusCode})");
            }
        }
        public static async Task<Result<TValue, TError>> ReceiveResult<TValue, TError>(this Task<IFlurlResponse> response,
            JsonSerializer serializer = null)
            where TError : IError
        {
            using var resp = await response.ConfigureAwait(false);
            return await Task.FromResult(resp.ResponseMessage).ReceiveResult<TValue, TError>();
        }
        private static TError ErrorResolver<TError>(HttpResponseMessage resp, TError error) where TError : IError
            => error == null ? (TError)(IError)ErrorResolver(resp) : error;
        private static Error ErrorResolver(HttpResponseMessage resp)
            => new Error(((int)resp.StatusCode).ToString(), resp.StatusCode.ToString());
    }
}
