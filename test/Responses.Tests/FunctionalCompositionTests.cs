using System;
using Xunit;
using static Responses.Tests.SerializationTest;

namespace Responses.Tests;

/// <summary>
/// Tests for synchronous functional composition: Map, Bind, Tap, Ensure.
/// Covers Result, Result&lt;T&gt;, and Result&lt;TValue, TError&gt;.
/// </summary>
public class FunctionalCompositionTests
{
    #region Map Tests

    public class MapTests
    {
        [Fact]
        public void Map_TransformsValue_WhenResultOfTIsSuccess()
        {
            var result = Result.Ok(5);
            var mapped = result.Map(x => x * 2);
            Assert.True(mapped.IsSuccess);
            Assert.Equal(10, mapped.Value);
        }

        [Fact]
        public void Map_PropagatesError_WhenResultOfTIsFailed()
        {
            var result = Result.Fail<int>("ERR", "original");
            var callCount = 0;
            var mapped = result.Map(x => { callCount++; return x * 2; });
            Assert.False(mapped.IsSuccess);
            Assert.Equal(0, callCount); // func was NOT called
            Assert.Equal("ERR", mapped.Error.Code);
            Assert.Equal("original", mapped.Error.Message);
        }

        [Fact]
        public void Map_ChangesType_WhenResultOfTIsSuccess()
        {
            var result = Result.Ok(42);
            var mapped = result.Map(x => x.ToString());
            Assert.True(mapped.IsSuccess);
            Assert.Equal("42", mapped.Value);
        }

        [Fact]
        public void Map_TransformsValue_WhenResultOfTValueTErrorIsSuccess()
        {
            var result = Result.Ok<int, TestError>(5);
            var mapped = result.Map(x => x * 3);
            Assert.True(mapped.IsSuccess);
            Assert.Equal(15, mapped.Value);
        }

        [Fact]
        public void Map_PropagatesTypedError_WhenResultOfTValueTErrorIsFailed()
        {
            var error = new TestError { Code = "TERR", Message = "typed" };
            var result = Result.Fail<int, TestError>(error);
            var callCount = 0;
            var mapped = result.Map(x => { callCount++; return x * 3; });
            Assert.False(mapped.IsSuccess);
            Assert.Equal(0, callCount);
            Assert.Equal("TERR", mapped.Error.Code);
        }

        [Fact]
        public void Map_ChainsMultipleMaps_WhenAllSuccess()
        {
            var result = Result.Ok(1);
            var mapped = result
                .Map(x => x + 1)
                .Map(x => x * 3)
                .Map(x => x - 2);
            Assert.True(mapped.IsSuccess);
            Assert.Equal(4, mapped.Value); // ((1+1)*3)-2 = 4
        }

        [Fact]
        public void Map_ShortCircuitsOnFirstFailure_InChain()
        {
            var callCount = 0;
            var result = Result.Ok(1)
                .Map(x => { callCount++; return x + 1; }) // called, result = 2
                .Bind(x => { callCount++; return Result.Fail<int>("STOP", "stop"); }) // called, returns fail
                .Map(x => { callCount++; return x * 10; }); // NOT called

            Assert.False(result.IsSuccess);
            Assert.Equal("STOP", result.Error.Code);
            Assert.Equal(2, callCount);
        }
    }

    #endregion

    #region Bind Tests

    public class BindTests
    {
        [Fact]
        public void Bind_ChainsFallibleOperation_WhenSuccess()
        {
            var result = Result.Ok(5);
            var bound = result.Bind(x => Result.Ok(x * 2));
            Assert.True(bound.IsSuccess);
            Assert.Equal(10, bound.Value);
        }

        [Fact]
        public void Bind_PropagatesError_WhenSourceIsFailed()
        {
            var result = Result.Fail<int>("SRC", "source error");
            var callCount = 0;
            var bound = result.Bind(x => { callCount++; return Result.Ok(x); });
            Assert.False(bound.IsSuccess);
            Assert.Equal(0, callCount);
            Assert.Equal("SRC", bound.Error.Code);
        }

        [Fact]
        public void Bind_PropagatesError_WhenFallibleOperationFails()
        {
            var result = Result.Ok(5);
            var bound = result.Bind(x => Result.Fail<int>("INNER", "inner error"));
            Assert.False(bound.IsSuccess);
            Assert.Equal("INNER", bound.Error.Code);
        }

        [Fact]
        public void Bind_RailwayPattern_ThreeOperations_SecondFails()
        {
            var log = "";
            var result = Result.Ok(1)
                .Bind(x => { log += "A"; return Result.Ok(x + 1); })
                .Bind(x => { log += "B"; return Result.Fail<int>("FAIL", "oops"); })
                .Bind(x => { log += "C"; return Result.Ok(x * 10); });

            Assert.False(result.IsSuccess);
            Assert.Equal("FAIL", result.Error.Code);
            Assert.Equal("AB", log); // C was NOT executed
        }

        [Fact]
        public void Bind_WithTypedError_PropagatesTypedError()
        {
            var error = new TestError { Code = "TERR", Message = "typed" };
            var result = Result.Fail<int, TestError>(error);
            var callCount = 0;
            var bound = result.Bind(x => { callCount++; return Result.Ok<int, TestError>(x); });
            Assert.False(bound.IsSuccess);
            Assert.Equal(0, callCount);
            Assert.Equal("TERR", bound.Error.Code);
        }

        [Fact]
        public void Bind_WithTypedError_ChainsSuccessfully()
        {
            var result = Result.Ok<int, TestError>(5);
            var bound = result.Bind(x => Result.Ok<int, TestError>(x * 2));
            Assert.True(bound.IsSuccess);
            Assert.Equal(10, bound.Value);
        }

        [Fact]
        public void Bind_VoidResult_ChainsOperations()
        {
            var log = "";
            var result = Result.Ok()
                .Bind(() => { log += "A"; return Result.Ok(); })
                .Bind(() => { log += "B"; return Result.Ok(); });
            Assert.True(result.IsSuccess);
            Assert.Equal("AB", log);
        }

        [Fact]
        public void Bind_VoidResult_ShortCircuitsOnFailure()
        {
            var log = "";
            var result = Result.Ok()
                .Bind(() => { log += "A"; return Result.Ok(); })
                .Bind(() => { log += "B"; return Result.Fail("ERR", "err"); })
                .Bind(() => { log += "C"; return Result.Ok(); });
            Assert.False(result.IsSuccess);
            Assert.Equal("AB", log);
        }
    }

    #endregion

    #region Tap Tests

    public class TapTests
    {
        [Fact]
        public void Tap_ExecutesAction_WhenResultOfTIsSuccess()
        {
            var sideEffect = 0;
            var result = Result.Ok(42);
            var tapped = result.Tap(x => sideEffect = x);
            Assert.Equal(42, sideEffect);
            Assert.True(tapped.IsSuccess);
            Assert.Equal(42, tapped.Value); // Tap doesn't change value
        }

        [Fact]
        public void Tap_DoesNotExecute_WhenResultOfTIsFailed()
        {
            var executed = false;
            var result = Result.Fail<int>("ERR", "msg");
            var tapped = result.Tap(x => executed = true);
            Assert.False(executed);
            Assert.True(tapped.IsFailed);
        }

        [Fact]
        public void Tap_ChainsMultipleTaps_WhenAllSuccess()
        {
            var log = "";
            var result = Result.Ok(10)
                .Tap(x => log += $"A:{x};")
                .Tap(x => log += $"B:{x * 2};");
            Assert.True(result.IsSuccess);
            Assert.Equal(10, result.Value);
            Assert.Equal("A:10;B:20;", log);
        }

        [Fact]
        public void Tap_SkipsAfterFailure_InChain()
        {
            var log = "";
            var result = Result.Ok(5)
                .Tap(x => log += "A")
                .Bind(x => Result.Fail<int>("STOP", "stop"))  // Bind, not Map — returns Result<int> (failed)
                .Tap(x => log += "B"); // should NOT execute

            Assert.True(result.IsFailed);
            Assert.Equal("A", log);
        }

        [Fact]
        public void Tap_WithTypedError_ExecutesOnSuccess()
        {
            var sideEffect = 0;
            var result = Result.Ok<int, TestError>(42);
            var tapped = result.Tap(x => sideEffect = x);
            Assert.Equal(42, sideEffect);
        }

        [Fact]
        public void Tap_WithTypedError_DoesNotExecuteOnFailure()
        {
            var executed = false;
            var error = new TestError { Code = "ERR", Message = "msg" };
            var result = Result.Fail<int, TestError>(error);
            var tapped = result.Tap(x => executed = true);
            Assert.False(executed);
        }

        [Fact]
        public void Tap_VoidResult_ExecutesAction()
        {
            var executed = false;
            var result = Result.Ok().Tap(() => executed = true);
            Assert.True(executed);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Tap_VoidResult_DoesNotExecuteOnFailure()
        {
            var executed = false;
            var result = Result.Fail("ERR", "msg").Tap(() => executed = true);
            Assert.False(executed);
        }
    }

    #endregion

    #region Ensure Tests

    public class EnsureTests
    {
        [Fact]
        public void Ensure_ReturnsSuccess_WhenPredicateIsTrue()
        {
            var result = Result.Ok(5).Ensure(x => x > 0, new Error("ERR", "must be positive"));
            Assert.True(result.IsSuccess);
            Assert.Equal(5, result.Value);
        }

        [Fact]
        public void Ensure_ReturnsFail_WhenPredicateIsFalse()
        {
            var result = Result.Ok(-1).Ensure(x => x > 0, new Error("NEG", "must be positive"));
            Assert.False(result.IsSuccess);
            Assert.Equal("NEG", result.Error.Code);
            Assert.Equal("must be positive", result.Error.Message);
        }

        [Fact]
        public void Ensure_PropagatesOriginalError_WhenAlreadyFailed()
        {
            var result = Result.Fail<int>("ORIGINAL", "original")
                .Ensure(x => x > 0, new Error("NEW", "new"));
            Assert.False(result.IsSuccess);
            Assert.Equal("ORIGINAL", result.Error.Code); // original error preserved
        }

        [Fact]
        public void Ensure_WithTypedError_ReturnsSuccess_WhenPredicateIsTrue()
        {
            var error = new TestError { Code = "ERR", Message = "err" };
            var result = Result.Ok<int, TestError>(5)
                .Ensure(x => x > 0, new TestError { Code = "NEG", Message = "negative" });
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Ensure_WithTypedError_ReturnsFail_WhenPredicateIsFalse()
        {
            var result = Result.Ok<int, TestError>(-1)
                .Ensure(x => x > 0, new TestError { Code = "NEG", Message = "negative" });
            Assert.False(result.IsSuccess);
            Assert.Equal("NEG", result.Error.Code);
        }

        [Fact]
        public void Ensure_WithTypedError_PropagatesOriginalError()
        {
            var originalError = new TestError { Code = "ORIG", Message = "original" };
            var result = Result.Fail<int, TestError>(originalError)
                .Ensure(x => x > 0, new TestError { Code = "NEW", Message = "new" });
            Assert.False(result.IsSuccess);
            Assert.Equal("ORIG", result.Error.Code);
        }

        [Fact]
        public void Ensure_ValidationChain_AllPass()
        {
            var result = Result.Ok("hello@example.com")
                .Ensure(s => !string.IsNullOrEmpty(s), new Error("EMPTY", "required"))
                .Ensure(s => s.Contains("@"), new Error("FMT", "invalid format"))
                .Ensure(s => s.Length >= 5, new Error("LEN", "too short"));
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Ensure_ValidationChain_SecondFails()
        {
            var result = Result.Ok("x")
                .Ensure(s => !string.IsNullOrEmpty(s), new Error("EMPTY", "required"))
                .Ensure(s => s.Contains("@"), new Error("FMT", "invalid format"))
                .Ensure(s => s.Length >= 5, new Error("LEN", "too short"));
            Assert.False(result.IsSuccess);
            Assert.Equal("FMT", result.Error.Code);
        }
    }

    #endregion
}
