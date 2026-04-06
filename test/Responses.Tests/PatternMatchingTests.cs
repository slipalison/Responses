using System;
using System.Threading.Tasks;
using Xunit;
using static Responses.Tests.SerializationTest;

namespace Responses.Tests;

/// <summary>
/// Tests for pattern matching: Match and Else.
/// Covers Result, Result&lt;T&gt;, and Result&lt;TValue, TError&gt;.
/// </summary>
public class PatternMatchingTests
{
    #region Match Tests

    public class MatchTests
    {
        [Fact]
        public void Match_ExecutesOnSuccess_WhenResultOfTIsSuccess()
        {
            var onSuccessCalled = false;
            var result = Result.Ok(42);
            result.Match(
                v => onSuccessCalled = v == 42,
                _ => { });
            Assert.True(onSuccessCalled);
        }

        [Fact]
        public void Match_ExecutesOnFailure_WhenResultOfTIsFailed()
        {
            var onFailureCalled = false;
            var result = Result.Fail<int>("ERR", "msg");
            result.Match(
                _ => { },
                e => onFailureCalled = e.Code == "ERR");
            Assert.True(onFailureCalled);
        }

        [Fact]
        public void Match_ReturnsTransformedValue_WhenSuccess()
        {
            var result = Result.Ok(42);
            var output = result.Match(
                v => $"ok: {v}",
                e => $"err: {e.Code}");
            Assert.Equal("ok: 42", output);
        }

        [Fact]
        public void Match_ReturnsErrorResult_WhenFailed()
        {
            var result = Result.Fail<int>("ERR", "msg");
            var output = result.Match(
                v => $"ok: {v}",
                e => $"err: {e.Code}");
            Assert.Equal("err: ERR", output);
        }

        [Fact]
        public void Match_VoidOverload_ExecutesCorrectBranch()
        {
            var log = "";
            Result.Ok(42).Match(
                v => log = $"success: {v}",
                e => log = $"failure: {e.Code}");
            Assert.Equal("success: 42", log);
        }

        [Fact]
        public void Match_VoidOverload_ExecutesFailureBranch()
        {
            var log = "";
            Result.Fail<int>("ERR", "msg").Match(
                v => log = $"success: {v}",
                e => log = $"failure: {e.Code}");
            Assert.Equal("failure: ERR", log);
        }

        [Fact]
        public void Match_WithTypedError_ExecutesSuccessBranch()
        {
            var result = Result.Ok<int, TestError>(42);
            var output = result.Match(
                v => $"ok: {v}",
                e => $"err: {e.Code}");
            Assert.Equal("ok: 42", output);
        }

        [Fact]
        public void Match_WithTypedError_ExecutesFailureBranch()
        {
            var error = new TestError { Code = "TERR", Message = "typed" };
            var result = Result.Fail<int, TestError>(error);
            var output = result.Match(
                v => $"ok: {v}",
                e => $"err: {e.Code}");
            Assert.Equal("err: TERR", output);
        }

        [Fact]
        public void Match_VoidResult_ExecutesSuccessBranch()
        {
            var log = "";
            Result.Ok().Match(
                () => log = "success",
                e => log = $"failure: {e.Code}");
            Assert.Equal("success", log);
        }

        [Fact]
        public void Match_VoidResult_ExecutesFailureBranch()
        {
            var log = "";
            Result.Fail("ERR", "msg").Match(
                () => log = "success",
                e => log = $"failure: {e.Code}");
            Assert.Equal("failure: ERR", log);
        }
    }

    #endregion

    #region Else Tests

    public class ElseTests
    {
        [Fact]
        public void Else_ReturnsValue_WhenResultOfTIsSuccess()
        {
            var result = Result.Ok(42);
            Assert.Equal(42, result.Else(0));
        }

        [Fact]
        public void Else_ReturnsFallback_WhenResultOfTIsFailed()
        {
            var result = Result.Fail<int>("ERR", "msg");
            Assert.Equal(99, result.Else(99));
        }

        [Fact]
        public void Else_WithFunc_ReturnsValue_WhenSuccess()
        {
            var fallbackCalled = false;
            var result = Result.Ok(42);
            var value = result.Else(e => { fallbackCalled = true; return 0; });
            Assert.Equal(42, value);
            Assert.False(fallbackCalled);
        }

        [Fact]
        public void Else_WithFunc_ExecutesFallback_WhenFailed()
        {
            var fallbackCode = "";
            var result = Result.Fail<int>("ERR", "msg");
            var value = result.Else(e => { fallbackCode = e.Code; return 99; });
            Assert.Equal(99, value);
            Assert.Equal("ERR", fallbackCode);
        }

        [Fact]
        public void Else_WithTypedError_ReturnsValue_WhenSuccess()
        {
            var result = Result.Ok<int, TestError>(42);
            Assert.Equal(42, result.Else(0));
        }

        [Fact]
        public void Else_WithTypedError_ReturnsFallback_WhenFailed()
        {
            var error = new TestError { Code = "TERR", Message = "typed" };
            var result = Result.Fail<int, TestError>(error);
            Assert.Equal(99, result.Else(99));
        }

        [Fact]
        public void Else_WithTypedErrorFunc_ExecutesFallback_WhenFailed()
        {
            var error = new TestError { Code = "TERR", Message = "typed" };
            var result = Result.Fail<int, TestError>(error);
            var value = result.Else(e => e.Code == "TERR" ? -1 : 0);
            Assert.Equal(-1, value);
        }
    }

    #endregion

    #region MatchAsync Tests

    public class MatchAsyncTests
    {
        [Fact]
        public async Task MatchAsync_ExecutesSuccessBranch_WhenSuccess()
        {
            var result = Result.Ok(42);
            var output = await result.MatchAsync(
                async v => { await Task.Delay(1); return $"ok: {v}"; },
                async e => { await Task.Delay(1); return $"err: {e.Code}"; });
            Assert.Equal("ok: 42", output);
        }

        [Fact]
        public async Task MatchAsync_ExecutesFailureBranch_WhenFailed()
        {
            var result = Result.Fail<int>("ERR", "msg");
            var output = await result.MatchAsync(
                async v => { await Task.Delay(1); return $"ok: {v}"; },
                async e => { await Task.Delay(1); return $"err: {e.Code}"; });
            Assert.Equal("err: ERR", output);
        }

        [Fact]
        public async Task MatchAsync_WithTypedError_ExecutesCorrectBranch()
        {
            var error = new TestError { Code = "TERR", Message = "typed" };
            var result = Result.Fail<int, TestError>(error);
            var output = await result.MatchAsync(
                async v => { await Task.Delay(1); return $"ok: {v}"; },
                async e => { await Task.Delay(1); return $"err: {e.Code}"; });
            Assert.Equal("err: TERR", output);
        }
    }

    #endregion

    #region ElseAsync Tests

    public class ElseAsyncTests
    {
        [Fact]
        public async Task ElseAsync_ReturnsValue_WhenSuccess()
        {
            var result = Result.Ok(42);
            var value = await result.ElseAsync(async e => { await Task.Delay(1); return 0; });
            Assert.Equal(42, value);
        }

        [Fact]
        public async Task ElseAsync_ExecutesFallback_WhenFailed()
        {
            var result = Result.Fail<int>("ERR", "msg");
            var value = await result.ElseAsync(async e => { await Task.Delay(1); return 99; });
            Assert.Equal(99, value);
        }

        [Fact]
        public async Task ElseAsync_WithTypedError_ExecutesFallback_WhenFailed()
        {
            var error = new TestError { Code = "TERR", Message = "typed" };
            var result = Result.Fail<int, TestError>(error);
            var value = await result.ElseAsync(async e => { await Task.Delay(1); return e.Code == "TERR" ? -1 : 0; });
            Assert.Equal(-1, value);
        }
    }

    #endregion
}
