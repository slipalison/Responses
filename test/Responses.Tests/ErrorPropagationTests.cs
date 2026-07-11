using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Responses.Tests;

/// <summary>
/// Covers how failures carry their error(s): custom <see cref="IError"/> acceptance,
/// multi-error preservation through composition, and coherent degenerate states.
/// </summary>
public class ErrorPropagationTests
{
    private sealed class CustomError : IError
    {
        public string Code => "CUSTOM";
        public string Message => "custom failure";
        public ErrorType Type => ErrorType.Conflict;
        public string Layer => "DomainLayer";
        public string ApplicationName => "DomainApp";
        public IReadOnlyDictionary<string, string> Metadata { get; } =
            new Dictionary<string, string> { ["k"] = "v" };
    }

    [Fact]
    public void Fail_WithCustomIError_DoesNotThrow_AndPreservesFields()
    {
        var result = Result.Fail(new CustomError());

        Assert.True(result.IsFailed);
        Assert.Equal("CUSTOM", result.Error.Code);
        Assert.Equal("custom failure", result.Error.Message);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("DomainLayer", result.Error.Layer);
        Assert.Equal("DomainApp", result.Error.ApplicationName);
        Assert.Equal("v", result.Error.Metadata["k"]);
    }

    [Fact]
    public void FailOfT_WithCustomIError_DoesNotThrow()
    {
        var result = Result.Fail<int>(new IError[] { new CustomError() });
        Assert.True(result.IsFailed);
        Assert.Equal("CUSTOM", result.Error.Code);
    }

    [Fact]
    public void Fail_ConcreteError_IsReturnedUnchanged()
    {
        var original = new Error("E", "m", ErrorType.NotFound);
        var result = Result.Fail(new IError[] { original });
        Assert.Equal(original.Code, result.Error.Code);
        Assert.Equal(original.Type, result.Error.Type);
    }

    private static readonly IError[] _threeErrors =
    {
        new Error("E1", "first"),
        new Error("E2", "second"),
        new Error("E3", "third"),
    };

    [Fact]
    public void Map_OnFailure_PreservesAllErrors()
    {
        var mapped = Result.Fail<int>(_threeErrors).Map(x => x * 2);
        Assert.Equal(3, mapped.Errors.Count);
    }

    [Fact]
    public void Bind_OnFailure_PreservesAllErrors()
    {
        var bound = Result.Fail<int>(_threeErrors).Bind(x => Result.Ok(x + 1));
        Assert.Equal(3, bound.Errors.Count);
    }

    [Fact]
    public async Task MapAsync_OnFailure_PreservesAllErrors()
    {
        var mapped = await Result.Fail<int>(_threeErrors).MapAsync(x => Task.FromResult(x * 2));
        Assert.Equal(3, mapped.Errors.Count);
    }

    [Fact]
    public async Task BindAsync_OnFailure_PreservesAllErrors()
    {
        var bound = await Result.Fail<int>(_threeErrors).BindAsync(x => Task.FromResult(Result.Ok(x)));
        Assert.Equal(3, bound.Errors.Count);
    }

    [Fact]
    public void SelectMany_OnFailure_PreservesAllErrors()
    {
        var projected =
            from x in Result.Fail<int>(_threeErrors)
            from y in Result.Ok(10)
            select x + y;
        Assert.Equal(3, projected.Errors.Count);
    }

    [Fact]
    public void TypedMap_OnFailure_PreservesAllErrors()
    {
        var mapped = Result.Fail<int, Error>(_threeErrors).Map(x => x * 2);
        Assert.Equal(3, mapped.Errors.Count);
    }

    [Fact]
    public void TypedBind_OnFailure_PreservesAllErrors()
    {
        var bound = Result.Fail<int, Error>(_threeErrors).Bind(x => Result.Ok<int, Error>(x));
        Assert.Equal(3, bound.Errors.Count);
    }
}
