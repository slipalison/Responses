using System.Text.Json;
using Responses.Serialization;
using Xunit;

namespace Responses.Tests;

public class StjDebugTests
{
    private static readonly JsonSerializerOptions Options = ResultJsonContext.DefaultOptions;

    [Fact]
    public void Debug_SerializeResultOk_ShowsJson()
    {
        var result = Result.Ok();
        var json = JsonSerializer.Serialize(result, ResultJsonContext.Default.Result);
        Assert.Contains("isSuccessful", json);
    }

    [Fact]
    public void Debug_DeserializeResultOk_RoundTrip()
    {
        var result = Result.Ok();
        var json = JsonSerializer.Serialize(result, ResultJsonContext.Default.Result);
        var back = JsonSerializer.Deserialize(json, ResultJsonContext.Default.Result);
        Assert.True(back!.IsSuccess);
    }

    [Fact]
    public void Debug_SerializeResultFail_RoundTrip()
    {
        var result = Result.Fail("ERR001", "msg");
        var json = JsonSerializer.Serialize(result, ResultJsonContext.Default.Result);
        var back = JsonSerializer.Deserialize(json, ResultJsonContext.Default.Result);
        Assert.True(back!.IsFailed);
        Assert.Equal("ERR001", back.Errors[0].Code);
    }

    [Fact]
    public void Debug_SerializeResultOfT_RoundTrip()
    {
        var result = Result.Ok(42);
        var json = JsonSerializer.Serialize(result, ResultJsonContext.Default.ResultInt32);
        var back = JsonSerializer.Deserialize(json, ResultJsonContext.Default.ResultInt32);
        Assert.True(back!.IsSuccess);
        Assert.Equal(42, back.ValueOrDefault);
    }

    [Fact]
    public void Debug_SerializeResultOfTString_RoundTrip()
    {
        var result = Result.Ok("hello");
        var json = JsonSerializer.Serialize(result, ResultJsonContext.Default.ResultString);
        var back = JsonSerializer.Deserialize(json, ResultJsonContext.Default.ResultString);
        Assert.True(back!.IsSuccess);
        Assert.Equal("hello", back.ValueOrDefault);
    }

    [Fact]
    public void Debug_SerializeResultOfTValueTError_RoundTrip()
    {
        var error = Error.Validation("VAL", "msg");
        var result = Result.Fail<int, Error>(error);
        var json = JsonSerializer.Serialize(result, ResultJsonContext.Default.ResultInt32Error);
        var back = JsonSerializer.Deserialize(json, ResultJsonContext.Default.ResultInt32Error);
        Assert.True(back!.IsFailed);
        Assert.Equal(ErrorType.Validation, back.Errors[0].Type);
    }
}
