using System;
using Xunit;
using static Responses.Tests.SerializationTest;

namespace Responses.Tests;

public class ResultFoundationTests
{
    #region Result (void) Tests

    public class ResultVoidTests
    {
        [Fact]
        public void Ok_CreatesSuccessResult()
        {
            var result = Result.Ok();
            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailed);
        }

        [Fact]
        public void FailWithString_CreatesFailedResultWithError()
        {
            var result = Result.Fail("ERR001", "Something went wrong");
            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailed);
            Assert.Equal("ERR001", result.Error.Code);
            Assert.Equal("Something went wrong", result.Error.Message);
        }

        [Fact]
        public void FailWithError_CreatesFailedResultWithError()
        {
            var error = new Error("ERR002", "Custom error");
            var result = Result.Fail(error);
            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailed);
            Assert.Equal("ERR002", result.Error.Code);
        }

        [Fact]
        public void Error_ThrowsInvalidOperationException_WhenSuccess()
        {
            var result = Result.Ok();
            Assert.Throws<InvalidOperationException>(() => result.Error);
        }

        [Fact]
        public void Error_ReturnsError_WhenFailed()
        {
            var result = Result.Fail("ERR", "msg");
            var error = result.Error;
            Assert.Equal("ERR", error.Code);
            Assert.Equal("msg", error.Message);
        }

        [Fact]
        public void ToString_ContainsSuccess_WhenOk()
        {
            var result = Result.Ok();
            Assert.Contains("Success", result.ToString());
        }

        [Fact]
        public void ToString_ContainsError_WhenFailed()
        {
            var result = Result.Fail("ERR", "msg");
            var str = result.ToString();
            Assert.Contains("Failed", str);
            Assert.Contains("ERR", str);
        }

        [Fact]
        public void Tap_ExecutesAction_WhenSuccess()
        {
            var executed = false;
            var result = Result.Ok().Tap(() => executed = true);
            Assert.True(executed);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Tap_DoesNotExecute_WhenFailed()
        {
            var executed = false;
            var result = Result.Fail("ERR", "msg").Tap(() => executed = true);
            Assert.False(executed);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public void Bind_ExecutesFunc_WhenSuccess()
        {
            var result = Result.Ok().Bind(() => Result.Ok());
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Bind_ShortCircuits_WhenFailed()
        {
            var executed = false;
            var result = Result.Fail("ERR", "msg").Bind(() => { executed = true; return Result.Ok(); });
            Assert.False(executed);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public void OkIf_True_ReturnsOk()
        {
            var result = Result.OkIf(true, "code", "msg");
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void OkIf_False_ReturnsFail()
        {
            var result = Result.OkIf(false, "code", "msg");
            Assert.True(result.IsFailed);
            Assert.Equal("code", result.Error.Code);
        }

        [Fact]
        public void FailIf_True_ReturnsFail()
        {
            var result = Result.FailIf(true, "code", "msg");
            Assert.True(result.IsFailed);
            Assert.Equal("code", result.Error.Code);
        }

        [Fact]
        public void FailIf_False_ReturnsOk()
        {
            var result = Result.FailIf(false, "code", "msg");
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Match_ExecutesOnSuccess_WhenSuccess()
        {
            var onSuccessCalled = false;
            Result.Ok().Match(() => onSuccessCalled = true, _ => { });
            Assert.True(onSuccessCalled);
        }

        [Fact]
        public void Match_ExecutesOnFailure_WhenFailed()
        {
            var onFailureCalled = false;
            Result.Fail("ERR", "msg").Match(() => { }, _ => onFailureCalled = true);
            Assert.True(onFailureCalled);
        }
    }

    #endregion

    #region Result<T> Tests

    public class ResultOfTTests
    {
        [Fact]
        public void Ok_CreatesSuccessResultWithValue()
        {
            var result = Result.Ok(42);
            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailed);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void Fail_CreatesFailedResultWithError()
        {
            var result = Result.Fail<int>("ERR", "msg");
            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailed);
            Assert.Equal("ERR", result.Error.Code);
        }

        [Fact]
        public void Value_ReturnsValue_WhenSuccess()
        {
            var result = Result.Ok("hello");
            Assert.Equal("hello", result.Value);
        }

        [Fact]
        public void Value_ThrowsInvalidOperationException_WhenFailed()
        {
            var result = Result.Fail<string>("ERR", "msg");
            Assert.Throws<InvalidOperationException>(() => result.Value);
        }

        [Fact]
        public void ValueOrDefault_ReturnsValue_WhenSuccess()
        {
            var result = Result.Ok(42);
            Assert.Equal(42, result.ValueOrDefault);
        }

        [Fact]
        public void ValueOrDefault_ReturnsDefault_WhenFailed()
        {
            var result = Result.Fail<int>("ERR", "msg");
            Assert.Equal(0, result.ValueOrDefault);
        }

        [Fact]
        public void ValueOrDefault_ReturnsNullForReferenceTypes_WhenFailed()
        {
            var result = Result.Fail<string>("ERR", "msg");
            Assert.Null(result.ValueOrDefault);
        }

        [Fact]
        public void Error_ThrowsInvalidOperationException_WhenSuccess()
        {
            var result = Result.Ok(42);
            Assert.Throws<InvalidOperationException>(() => result.Error);
        }

        [Fact]
        public void Error_ReturnsError_WhenFailed()
        {
            var result = Result.Fail<int>("ERR", "msg");
            Assert.Equal("ERR", result.Error.Code);
            Assert.Equal("msg", result.Error.Message);
        }

        [Fact]
        public void ToString_ContainsValue_WhenSuccess()
        {
            var result = Result.Ok(42);
            Assert.Contains("42", result.ToString());
            Assert.Contains("Success", result.ToString());
        }

        [Fact]
        public void ToString_ContainsError_WhenFailed()
        {
            var result = Result.Fail<int>("ERR", "msg");
            var str = result.ToString();
            Assert.Contains("Failed", str);
            Assert.Contains("ERR", str);
        }

        [Fact]
        public void Map_TransformsValue_WhenSuccess()
        {
            var result = Result.Ok(5);
            var mapped = result.Map(x => x * 2);
            Assert.True(mapped.IsSuccess);
            Assert.Equal(10, mapped.Value);
        }

        [Fact]
        public void Map_PropagatesError_WhenFailed()
        {
            var result = Result.Fail<int>("ERR", "msg");
            var mapped = result.Map(x => x * 2);
            Assert.False(mapped.IsSuccess);
            Assert.Equal("ERR", mapped.Error.Code);
        }

        [Fact]
        public void Bind_ChainsOperation_WhenSuccess()
        {
            var result = Result.Ok(5);
            var bound = result.Bind(x => Result.Ok(x * 2));
            Assert.True(bound.IsSuccess);
            Assert.Equal(10, bound.Value);
        }

        [Fact]
        public void Bind_ShortCircuits_WhenFailed()
        {
            var callCount = 0;
            var result = Result.Fail<int>("ERR", "msg");
            var bound = result
                .Bind(x => { callCount++; return Result.Ok(x + 1); })
                .Bind(x => { callCount++; return Result.Ok(x * 2); });

            Assert.True(bound.IsFailed);
            Assert.Equal(0, callCount);
        }

        [Fact]
        public void Tap_ExecutesAction_WhenSuccess()
        {
            var executed = false;
            var result = Result.Ok(42).Tap(x => executed = x == 42);
            Assert.True(executed);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Tap_DoesNotExecute_WhenFailed()
        {
            var executed = false;
            var result = Result.Fail<int>("ERR", "msg").Tap(x => executed = true);
            Assert.False(executed);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public void Ensure_ReturnsSuccess_WhenPredicateIsTrue()
        {
            var result = Result.Ok(5).Ensure(x => x > 0, new Error("ERR", "msg"));
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Ensure_ReturnsFail_WhenPredicateIsFalse()
        {
            var result = Result.Ok(-1).Ensure(x => x > 0, new Error("ERR", "must be positive"));
            Assert.True(result.IsFailed);
            Assert.Equal("ERR", result.Error.Code);
        }

        [Fact]
        public void Else_ReturnsValue_WhenSuccess()
        {
            var result = Result.Ok(42);
            Assert.Equal(42, result.Else(0));
        }

        [Fact]
        public void Else_ReturnsFallback_WhenFailed()
        {
            var result = Result.Fail<int>("ERR", "msg");
            Assert.Equal(99, result.Else(99));
        }

        [Fact]
        public void OkIf_True_ReturnsOkWithValue()
        {
            var result = Result.OkIf(true, 42, "code", "msg");
            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void OkIf_False_ReturnsFail()
        {
            var result = Result.OkIf(false, 42, "code", "msg");
            Assert.True(result.IsFailed);
            Assert.Equal("code", result.Error.Code);
        }

        [Fact]
        public void FailIf_True_ReturnsFail()
        {
            var result = Result.FailIf(true, 42, "code", "msg");
            Assert.True(result.IsFailed);
            Assert.Equal("code", result.Error.Code);
        }

        [Fact]
        public void FailIf_False_ReturnsOkWithValue()
        {
            var result = Result.FailIf(false, 42, "code", "msg");
            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void Match_ReturnsTransformedValue_WhenSuccess()
        {
            var result = Result.Ok(42);
            var output = result.Match(x => $"ok: {x}", e => $"err: {e.Code}");
            Assert.Equal("ok: 42", output);
        }

        [Fact]
        public void Match_ReturnsErrorResult_WhenFailed()
        {
            var result = Result.Fail<int>("ERR", "msg");
            var output = result.Match(x => $"ok: {x}", e => $"err: {e.Code}");
            Assert.Equal("err: ERR", output);
        }

        [Fact]
        public void SelectMany_ChainsMultipleResults_WhenAllSuccess()
        {
            var query = from x in Result.Ok(5)
                        from y in Result.Ok(10)
                        select x + y;
            Assert.True(query.IsSuccess);
            Assert.Equal(15, query.Value);
        }

        [Fact]
        public void SelectMany_ShortCircuits_WhenAnyFails()
        {
            var callCount = 0;
            var query = from x in Result.Fail<int>("ERR", "msg")
                        from y in Result.Ok(10).Tap(_ => callCount++)
                        select x + y;

            Assert.True(query.IsFailed);
            Assert.Equal(0, callCount);
        }
    }

    #endregion

    #region Result<TValue, TError> Tests

    public class ResultOfTValueTErrorTests
    {
        [Fact]
        public void Ok_CreatesSuccessResultWithTypedError()
        {
            var result = Result.Ok<int, TestError>(42);
            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailed);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void Fail_CreatesFailedResultWithTypedError()
        {
            var error = new TestError { Code = "ERR", Message = "typed error" };
            var result = Result.Fail<int, TestError>(error);
            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailed);
            Assert.Equal("ERR", result.Error.Code);
        }

        [Fact]
        public void ValueOrDefault_ReturnsValue_WhenSuccess()
        {
            var result = Result.Ok<int, TestError>(42);
            Assert.Equal(42, result.ValueOrDefault);
        }

        [Fact]
        public void ValueOrDefault_ReturnsDefault_WhenFailed()
        {
            var error = new TestError { Code = "ERR", Message = "msg" };
            var result = Result.Fail<int, TestError>(error);
            Assert.Equal(0, result.ValueOrDefault);
        }

        [Fact]
        public void Map_TransformsValue_WhenSuccess()
        {
            var result = Result.Ok<int, TestError>(5);
            var mapped = result.Map(x => x * 2);
            Assert.True(mapped.IsSuccess);
            Assert.Equal(10, mapped.Value);
        }

        [Fact]
        public void Map_PropagatesError_WhenFailed()
        {
            var error = new TestError { Code = "ERR", Message = "msg" };
            var result = Result.Fail<int, TestError>(error);
            var mapped = result.Map(x => x * 2);
            Assert.False(mapped.IsSuccess);
            Assert.Equal("ERR", mapped.Error.Code);
        }

        [Fact]
        public void Bind_ChainsOperation_WhenSuccess()
        {
            var result = Result.Ok<int, TestError>(5);
            var bound = result.Bind(x => Result.Ok<int, TestError>(x * 2));
            Assert.True(bound.IsSuccess);
            Assert.Equal(10, bound.Value);
        }

        [Fact]
        public void Bind_ShortCircuits_WhenFailed()
        {
            var callCount = 0;
            var error = new TestError { Code = "ERR", Message = "msg" };
            var result = Result.Fail<int, TestError>(error);
            var bound = result
                .Bind(x => { callCount++; return Result.Ok<int, TestError>(x + 1); })
                .Bind(x => { callCount++; return Result.Ok<int, TestError>(x * 2); });

            Assert.True(bound.IsFailed);
            Assert.Equal(0, callCount);
        }

        [Fact]
        public void Tap_ExecutesAction_WhenSuccess()
        {
            var executed = false;
            var result = Result.Ok<int, TestError>(42).Tap(x => executed = x == 42);
            Assert.True(executed);
        }

        [Fact]
        public void Tap_DoesNotExecute_WhenFailed()
        {
            var executed = false;
            var error = new TestError { Code = "ERR", Message = "msg" };
            var result = Result.Fail<int, TestError>(error).Tap(x => executed = true);
            Assert.False(executed);
        }

        [Fact]
        public void Else_ReturnsValue_WhenSuccess()
        {
            var result = Result.Ok<int, TestError>(42);
            Assert.Equal(42, result.Else(0));
        }

        [Fact]
        public void Else_ReturnsFallback_WhenFailed()
        {
            var error = new TestError { Code = "ERR", Message = "msg" };
            var result = Result.Fail<int, TestError>(error);
            Assert.Equal(99, result.Else(99));
        }

        [Fact]
        public void OkIf_True_ReturnsOk()
        {
            var error = new TestError { Code = "ERR", Message = "msg" };
            var result = Result.OkIf<int, TestError>(true, 42, error);
            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void OkIf_False_ReturnsFail()
        {
            var error = new TestError { Code = "ERR", Message = "msg" };
            var result = Result.OkIf<int, TestError>(false, 42, error);
            Assert.True(result.IsFailed);
            Assert.Equal("ERR", result.Error.Code);
        }

        [Fact]
        public void FailIf_True_ReturnsFail()
        {
            var error = new TestError { Code = "ERR", Message = "msg" };
            var result = Result.FailIf<int, TestError>(true, 42, error);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public void FailIf_False_ReturnsOk()
        {
            var result = Result.FailIf<int, TestError>(false, 42, new TestError());
            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }
    }

    #endregion
}
