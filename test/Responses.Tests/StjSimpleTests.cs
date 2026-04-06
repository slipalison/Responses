using System.Text.Json;
using Xunit;

namespace Responses.Tests;

public class StjSimpleTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    [Fact]
    public void Simple_ResultOk_RoundTrip()
    {
        var result = Result.Ok();
        var json = JsonSerializer.Serialize(result, Options);
        var back = JsonSerializer.Deserialize<Result>(json, Options);
        Assert.True(back.IsSuccess);
    }

    [Fact]
    public void Simple_ResultFail_RoundTrip()
    {
        var result = Result.Fail("ERR", "msg");
        var json = JsonSerializer.Serialize(result, Options);
        var back = JsonSerializer.Deserialize<Result>(json, Options);
        Assert.True(back.IsFailed);
        Assert.Equal("ERR", back.Errors[0].Code);
    }

    [Fact]
    public void Simple_ResultOfT_RoundTrip()
    {
        var result = Result.Ok(42);
        var json = JsonSerializer.Serialize(result, Options);
        var back = JsonSerializer.Deserialize<Result<int>>(json, Options);
        Assert.True(back.IsSuccess);
        Assert.Equal(42, back.ValueOrDefault);
    }

    [Fact]
    public void Simple_ResultOfTString_RoundTrip()
    {
        var result = Result.Ok("hello");
        var json = JsonSerializer.Serialize(result, Options);
        var back = JsonSerializer.Deserialize<Result<string>>(json, Options);
        Assert.True(back.IsSuccess);
        Assert.Equal("hello", back.ValueOrDefault);
    }
}
