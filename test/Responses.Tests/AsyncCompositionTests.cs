using System;
using System.Threading.Tasks;
using Xunit;
using static Responses.Tests.SerializationTest;

namespace Responses.Tests;

/// <summary>
/// Tests for async functional composition: MapAsync, BindAsync, TapAsync, EnsureAsync.
/// Covers Result, Result&lt;T&gt;, and Result&lt;TValue, TError&gt;.
/// </summary>
public class AsyncCompositionTests
{
    #region MapAsync Tests

    public class MapAsyncTests
    {
        [Fact]
        public async Task MapAsync_TransformsValue_WhenResultOfTIsSuccess()
        {
            var result = Result.Ok(5);
            var mapped = await result.MapAsync(async x =>
            {
                await Task.Delay(1);
                return x * 2;
            });
            Assert.True(mapped.IsSuccess);
            Assert.Equal(10, mapped.Value);
        }

        [Fact]
        public async Task MapAsync_DoesNotCallFunc_WhenResultOfTIsFailed()
        {
            var result = Result.Fail<int>("ERR", "msg");
            var callCount = 0;
            var mapped = await result.MapAsync(async x =>
            {
                callCount++;
                await Task.Delay(1);
                return x * 2;
            });
            Assert.False(mapped.IsSuccess);
            Assert.Equal(0, callCount);
        }

        [Fact]
        public async Task MapAsync_ChangesType_WhenSuccess()
        {
            var result = Result.Ok(42);
            var mapped = await result.MapAsync(async x =>
            {
                await Task.Delay(1);
                return x.ToString();
            });
            Assert.True(mapped.IsSuccess);
            Assert.Equal("42", mapped.Value);
        }

        [Fact]
        public async Task MapAsync_WithTypedError_TransformsValue()
        {
            var result = Result.Ok<int, TestError>(5);
            var mapped = await result.MapAsync(async x =>
            {
                await Task.Delay(1);
                return x * 3;
            });
            Assert.True(mapped.IsSuccess);
            Assert.Equal(15, mapped.Value);
        }

        [Fact]
        public async Task MapAsync_WithTypedError_DoesNotCallFunc_WhenFailed()
        {
            var error = new TestError { Code = "ERR", Message = "msg" };
            var result = Result.Fail<int, TestError>(error);
            var callCount = 0;
            var mapped = await result.MapAsync(async x =>
            {
                callCount++;
                await Task.Delay(1);
                return x;
            });
            Assert.False(mapped.IsSuccess);
            Assert.Equal(0, callCount);
        }
    }

    #endregion

    #region BindAsync Tests

    public class BindAsyncTests
    {
        [Fact]
        public async Task BindAsync_ChainsAsyncOperation_WhenSuccess()
        {
            var result = Result.Ok(5);
            var bound = await result.BindAsync(async x =>
            {
                await Task.Delay(1);
                return Result.Ok(x * 2);
            });
            Assert.True(bound.IsSuccess);
            Assert.Equal(10, bound.Value);
        }

        [Fact]
        public async Task BindAsync_DoesNotCallFunc_WhenSourceIsFailed()
        {
            var result = Result.Fail<int>("SRC", "source");
            var callCount = 0;
            var bound = await result.BindAsync(async x =>
            {
                callCount++;
                await Task.Delay(1);
                return Result.Ok(x);
            });
            Assert.False(bound.IsSuccess);
            Assert.Equal(0, callCount);
        }

        [Fact]
        public async Task BindAsync_PropagatesInnerFailure_WhenAsyncOperationFails()
        {
            var result = Result.Ok(5);
            var bound = await result.BindAsync(async x =>
            {
                await Task.Delay(1);
                return Result.Fail<int>("INNER", "inner error");
            });
            Assert.False(bound.IsSuccess);
            Assert.Equal("INNER", bound.Error.Code);
        }

        [Fact]
        public async Task BindAsync_RailwayPattern_SecondOperationFails()
        {
            var log = "";
            var result = Result.Ok(1);
            var first = await result.BindAsync(async x => { log += "A"; await Task.Delay(1); return Result.Ok(x + 1); });
            var second = await first.BindAsync(async x => { log += "B"; await Task.Delay(1); return Result.Fail<int>("FAIL", "fail"); });
            var third = await second.BindAsync(async x => { log += "C"; await Task.Delay(1); return Result.Ok(x * 10); });

            Assert.False(third.IsSuccess);
            Assert.Equal("FAIL", third.Error.Code);
            Assert.Equal("AB", log);
        }

        [Fact]
        public async Task BindAsync_WithTypedError_ChainsSuccessfully()
        {
            var result = Result.Ok<int, TestError>(5);
            var bound = await result.BindAsync(async x =>
            {
                await Task.Delay(1);
                return Result.Ok<int, TestError>(x * 2);
            });
            Assert.True(bound.IsSuccess);
            Assert.Equal(10, bound.Value);
        }

        [Fact]
        public async Task BindAsync_WithTypedError_DoesNotCallFunc_WhenFailed()
        {
            var error = new TestError { Code = "ERR", Message = "msg" };
            var result = Result.Fail<int, TestError>(error);
            var callCount = 0;
            var bound = await result.BindAsync(async x =>
            {
                callCount++;
                await Task.Delay(1);
                return Result.Ok<int, TestError>(x);
            });
            Assert.False(bound.IsSuccess);
            Assert.Equal(0, callCount);
        }

        [Fact]
        public async Task BindAsync_VoidResult_ChainsAsyncOperations()
        {
            var log = "";
            var result = Result.Ok();
            var first = await result.BindAsync(async () => { log += "A"; await Task.Delay(1); return Result.Ok(); });
            var second = await first.BindAsync(async () => { log += "B"; await Task.Delay(1); return Result.Ok(); });
            Assert.True(second.IsSuccess);
            Assert.Equal("AB", log);
        }

        [Fact]
        public async Task BindAsync_VoidResult_ShortCircuitsOnFailure()
        {
            var log = "";
            var result = Result.Ok();
            var first = await result.BindAsync(async () => { log += "A"; await Task.Delay(1); return Result.Ok(); });
            var second = await first.BindAsync(async () => { log += "B"; await Task.Delay(1); return Result.Fail("ERR", "err"); });
            var third = await second.BindAsync(async () => { log += "C"; await Task.Delay(1); return Result.Ok(); });
            Assert.False(third.IsSuccess);
            Assert.Equal("AB", log);
        }
    }

    #endregion

    #region TapAsync Tests

    public class TapAsyncTests
    {
        [Fact]
        public async Task TapAsync_ExecutesAction_WhenResultOfTIsSuccess()
        {
            var sideEffect = 0;
            var result = Result.Ok(42);
            var tapped = await result.TapAsync(async x =>
            {
                await Task.Delay(1);
                sideEffect = x;
            });
            Assert.Equal(42, sideEffect);
            Assert.True(tapped.IsSuccess);
            Assert.Equal(42, tapped.Value);
        }

        [Fact]
        public async Task TapAsync_DoesNotExecute_WhenResultOfTIsFailed()
        {
            var executed = false;
            var result = Result.Fail<int>("ERR", "msg");
            var tapped = await result.TapAsync(async x =>
            {
                await Task.Delay(1);
                executed = true;
            });
            Assert.False(executed);
            Assert.True(tapped.IsFailed);
        }

        [Fact]
        public async Task TapAsync_WithTypedError_ExecutesOnSuccess()
        {
            var sideEffect = 0;
            var result = Result.Ok<int, TestError>(42);
            var tapped = await result.TapAsync(async x =>
            {
                await Task.Delay(1);
                sideEffect = x;
            });
            Assert.Equal(42, sideEffect);
        }

        [Fact]
        public async Task TapAsync_WithTypedError_DoesNotExecuteOnFailure()
        {
            var executed = false;
            var error = new TestError { Code = "ERR", Message = "msg" };
            var result = Result.Fail<int, TestError>(error);
            var tapped = await result.TapAsync(async x =>
            {
                await Task.Delay(1);
                executed = true;
            });
            Assert.False(executed);
        }

        [Fact]
        public async Task TapAsync_VoidResult_ExecutesAction()
        {
            var executed = false;
            var result = Result.Ok();
            var tapped = await result.TapAsync(async () =>
            {
                await Task.Delay(1);
                executed = true;
            });
            Assert.True(executed);
            Assert.True(tapped.IsSuccess);
        }
    }

    #endregion

    #region EnsureAsync Tests

    public class EnsureAsyncTests
    {
        [Fact]
        public async Task EnsureAsync_ReturnsSuccess_WhenPredicateIsTrue()
        {
            var result = Result.Ok(5);
            var ensured = await result.EnsureAsync(x => x > 0, new Error("ERR", "must be positive"));
            Assert.True(ensured.IsSuccess);
            Assert.Equal(5, ensured.Value);
        }

        [Fact]
        public async Task EnsureAsync_ReturnsFail_WhenPredicateIsFalse()
        {
            var result = Result.Ok(-1);
            var ensured = await result.EnsureAsync(x => x > 0, new Error("NEG", "must be positive"));
            Assert.False(ensured.IsSuccess);
            Assert.Equal("NEG", ensured.Error.Code);
        }

        [Fact]
        public async Task EnsureAsync_PropagatesOriginalError_WhenAlreadyFailed()
        {
            var result = Result.Fail<int>("ORIGINAL", "original");
            var ensured = await result.EnsureAsync(x => x > 0, new Error("NEW", "new"));
            Assert.False(ensured.IsSuccess);
            Assert.Equal("ORIGINAL", ensured.Error.Code);
        }

        [Fact]
        public async Task EnsureAsync_WithTypedError_ReturnsSuccess_WhenPredicateIsTrue()
        {
            var result = Result.Ok<int, TestError>(5);
            var ensured = await result.EnsureAsync(x => x > 0, new TestError { Code = "NEG", Message = "neg" });
            Assert.True(ensured.IsSuccess);
        }

        [Fact]
        public async Task EnsureAsync_WithTypedError_ReturnsFail_WhenPredicateIsFalse()
        {
            var result = Result.Ok<int, TestError>(-1);
            var ensured = await result.EnsureAsync(x => x > 0, new TestError { Code = "NEG", Message = "neg" });
            Assert.False(ensured.IsSuccess);
            Assert.Equal("NEG", ensured.Error.Code);
        }
    }

    #endregion
}
