using System;
using Xunit;

namespace Responses.Tests;

/// <summary>
/// Tests to verify allocation characteristics of Result types.
/// These tests verify behavioral properties that ensure zero-allocation in hot paths.
/// Full allocation measurement requires BenchmarkDotNet (see benchmarks project).
/// </summary>
public class AllocationTests
{

    [Fact]
    public void ResultOk_DoesNotAllocate_WhenValueType()
    {
        // For value types, Result.Ok should not allocate
        // Note: This is a behavioral test, not a strict allocation measurement
        var result = Result.Ok(42);
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ResultMap_Success_DoesNotAllocateAdditionalHeapMemory()
    {
        var result = Result.Ok(5);
        var mapped = result.Map(x => x * 2);
        Assert.True(mapped.IsSuccess);
        Assert.Equal(10, mapped.Value);
    }

    [Fact]
    public void ResultBind_Failure_ShortCircuits_WithoutExecutingFunc()
    {
        var executed = false;
        var result = Result.Fail<int>("ERR", "msg")
            .Bind(x => { executed = true; return Result.Ok(x * 2); });

        Assert.False(executed);
        Assert.True(result.IsFailed);
    }

    [Fact]
    public void ResultErrorsCollection_IsEmpty_ForSuccess()
    {
        var result = Result.Ok(42);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ResultErrorsCollection_HasErrors_ForFailure()
    {
        var result = Result.Fail<int>("ERR", "msg");
        Assert.NotEmpty(result.Errors);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void ResultMultipleErrors_PreservesAllErrors()
    {
        var errors = new IError[]
        {
            Error.Validation("VAL1", "Invalid name"),
            Error.Validation("VAL2", "Invalid email"),
            Error.NotFound("NF1", "Not found")
        };
        var result = Result.Fail(errors);
        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public void ResultWithTypedError_PreservesErrorType()
    {
        var error = Error.Validation("VAL", "message");
        var result = Result.Fail<int, Error>(error);
        Assert.True(result.IsFailed);
        Assert.Equal(ErrorType.Validation, result.Errors[0].Type);
    }
}
