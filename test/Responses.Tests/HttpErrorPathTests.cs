using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Responses;
using Responses.Http;
using Xunit;

namespace Responses.Tests;

/// <summary>
/// Exercises the transport-failure catch blocks of the ReceiveResult extensions by awaiting
/// faulted HttpResponseMessage tasks directly — Flurl's HttpTest cannot simulate these.
/// </summary>
public class HttpErrorPathTests
{
    private static Task<HttpResponseMessage> Faulted(Exception ex) =>
        Task.FromException<HttpResponseMessage>(ex);

    [Fact]
    public async Task Void_NetworkError_MapsToServerError()
    {
        var result = await Faulted(new HttpRequestException("down")).ReceiveResult();
        Assert.True(result.IsFailed);
        Assert.Equal("HttpNetworkError", result.Error.Code);
        Assert.Equal(ErrorType.ServerError, result.Error.Type);
    }

    [Fact]
    public async Task Void_IoError_MapsToServerError()
    {
        var result = await Faulted(new IOException("socket")).ReceiveResult();
        Assert.Equal("HttpNetworkError", result.Error.Code);
    }

    [Fact]
    public async Task Void_Cancellation_MapsToCancelled()
    {
        var result = await Faulted(new OperationCanceledException()).ReceiveResult();
        Assert.True(result.IsFailed);
        Assert.Equal("HttpCancelled", result.Error.Code);
        Assert.Equal(ErrorType.Cancelled, result.Error.Type);
    }

    [Fact]
    public async Task Void_GenericError_MapsToHttpError()
    {
        var result = await Faulted(new InvalidOperationException("boom")).ReceiveResult();
        Assert.Equal("HttpError", result.Error.Code);
    }

    [Fact]
    public async Task OfT_NetworkError_MapsToServerError()
    {
        var result = await Faulted(new HttpRequestException("down")).ReceiveResult<int>();
        Assert.Equal("HttpNetworkError", result.Error.Code);
    }

    [Fact]
    public async Task OfT_Cancellation_MapsToCancelled()
    {
        var result = await Faulted(new OperationCanceledException()).ReceiveResult<int>();
        Assert.Equal("HttpCancelled", result.Error.Code);
    }

    [Fact]
    public async Task OfT_GenericError_MapsToHttpError()
    {
        var result = await Faulted(new InvalidOperationException("boom")).ReceiveResult<int>();
        Assert.Equal("HttpError", result.Error.Code);
    }

    [Fact]
    public async Task Typed_NetworkError_MapsToServerError()
    {
        var result = await Faulted(new HttpRequestException("down")).ReceiveResult<int, Error>();
        Assert.True(result.IsFailed);
        Assert.Equal("HttpNetworkError", result.Error.Code);
    }

    [Fact]
    public async Task Typed_Cancellation_MapsToCancelled()
    {
        var result = await Faulted(new OperationCanceledException()).ReceiveResult<int, Error>();
        Assert.Equal("HttpCancelled", result.Error.Code);
    }

    [Fact]
    public async Task Typed_GenericError_MapsToHttpError()
    {
        var result = await Faulted(new InvalidOperationException("boom")).ReceiveResult<int, Error>();
        Assert.Equal("HttpError", result.Error.Code);
    }

    [Fact]
    public async Task WithInfo_NetworkError_ReturnsFailAndDefaultInfo()
    {
        var (result, info) = await Faulted(new HttpRequestException("down")).ReceiveResultWithInfo<int>();
        Assert.True(result.IsFailed);
        Assert.Equal("HttpNetworkError", result.Error.Code);
        Assert.Equal(default, info.StatusCode);
    }

    [Fact]
    public async Task WithInfo_Cancellation_ReturnsCancelled()
    {
        var (result, _) = await Faulted(new OperationCanceledException()).ReceiveResultWithInfo<int>();
        Assert.Equal("HttpCancelled", result.Error.Code);
    }

    [Fact]
    public async Task WithInfo_GenericError_ReturnsHttpError()
    {
        var (result, _) = await Faulted(new InvalidOperationException("boom")).ReceiveResultWithInfo<int>();
        Assert.Equal("HttpError", result.Error.Code);
    }

    [Fact]
    public async Task NullResponseTask_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => ((Task<HttpResponseMessage>)null!).ReceiveResult());
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => ((Task<HttpResponseMessage>)null!).ReceiveResult<int>());
    }
}
