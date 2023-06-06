using System;
using Xunit;

namespace Responses.Tests;

public class StaticFactoriesTests
{
    [Fact]
    public void Ok()
    {
        Assert.True(Result.Ok().IsSuccess);
    }

    [Fact]
    public void Fail()
    {
        var result = Result.Fail("0001", nameof(Error));

        Assert.False(result.IsSuccess);
        Assert.Equal("0001", result.Error.Code);
    }

    [Fact]
    public void FailNamedTuple()
    {
        var errorMessage = (Code: "0001", Message: nameof(Error));

        var result = Result.Fail(errorMessage);

        Assert.False(result.IsSuccess);
        Assert.Equal("0001", result.Error.Code);
    }

    [Fact]
    public void FailTuple()
    {
        var errorMessage = new Tuple<string, string>("0001", nameof(Error));

        var result = Result.Fail(errorMessage);

        Assert.False(result.IsSuccess);
        Assert.Equal("0001", result.Error.Code);
    }

    [Fact]
    public void OkWithValue()
    {
        var result = Result.Ok(true);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public void FailWithValue()
    {
        var result = Result.Fail<bool>("0001", nameof(Error));

        Assert.False(result.IsSuccess);
        Assert.Equal("0001", result.Error.Code);
    }

    [Fact]
    public void FailWithValueNamedTuple()
    {
        var errorMessage = (Code: "0001", Message: nameof(Error));

        var result = Result.Fail<bool>(errorMessage);

        Assert.False(result.IsSuccess);
        Assert.Equal("0001", result.Error.Code);
    }

    [Fact]
    public void FailWithValueTuple()
    {
        var errorMessage = new Tuple<string, string>("0001", nameof(Error));

        var result = Result.Fail<bool>(errorMessage);

        Assert.False(result.IsSuccess);
        Assert.Equal("0001", result.Error.Code);
    }
}
