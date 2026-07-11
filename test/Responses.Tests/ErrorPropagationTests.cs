using System.Collections.Generic;
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
}
