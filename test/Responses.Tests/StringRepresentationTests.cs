using Xunit;
using static Responses.Tests.SerializationTest;

namespace Responses.Tests;

/// <summary>
/// Tests for string representation and conditional factories: ToString, OkIf, FailIf.
/// </summary>
public class StringRepresentationTests
{
    #region ToString Tests

    public class ToStringTests
    {
        [Fact]
        public void ToString_VoidResult_ContainsSuccess_WhenOk()
        {
            var result = Result.Ok();
            var str = result.ToString();
            Assert.Contains("Success", str);
            Assert.DoesNotContain("Failed", str);
        }

        [Fact]
        public void ToString_VoidResult_ContainsError_WhenFailed()
        {
            var result = Result.Fail("ERR001", "something went wrong");
            var str = result.ToString();
            Assert.Contains("Failed", str);
            Assert.Contains("ERR001", str);
        }

        [Fact]
        public void ToString_VoidResult_DoesNotThrow()
        {
            var ok = Result.Ok();
            var fail = Result.Fail("ERR", "msg");
            var ex = Record.Exception(() =>
            {
                var _ = ok.ToString();
                var __ = fail.ToString();
            });
            Assert.Null(ex);
        }

        [Fact]
        public void ToString_ResultOfT_ContainsValue_WhenSuccess()
        {
            var result = Result.Ok(42);
            var str = result.ToString();
            Assert.Contains("42", str);
            Assert.Contains("Success", str);
        }

        [Fact]
        public void ToString_ResultOfT_ContainsError_WhenFailed()
        {
            var result = Result.Fail<int>("ERR", "msg");
            var str = result.ToString();
            Assert.Contains("Failed", str);
            Assert.Contains("ERR", str);
            Assert.Contains("msg", str);
        }

        [Fact]
        public void ToString_ResultOfT_StringValue()
        {
            var result = Result.Ok("hello");
            var str = result.ToString();
            Assert.Contains("hello", str);
        }

        [Fact]
        public void ToString_ResultOfT_NullValue_ShowsDefault()
        {
            var result = Result.Ok<string>(null!);
            var ex = Record.Exception(() => result.ToString());
            Assert.Null(ex); // Should not throw
        }

        [Fact]
        public void ToString_ResultOfTValueTError_ContainsValue_WhenSuccess()
        {
            var result = Result.Ok<int, TestError>(42);
            var str = result.ToString();
            Assert.Contains("42", str);
            Assert.Contains("Success", str);
        }

        [Fact]
        public void ToString_ResultOfTValueTError_ContainsError_WhenFailed()
        {
            var error = new TestError { Code = "TERR", Message = "typed error" };
            var result = Result.Fail<int, TestError>(error);
            var str = result.ToString();
            Assert.Contains("Failed", str);
        }
    }

    #endregion

    #region OkIf Tests

    public class OkIfTests
    {
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
            Assert.Equal("msg", result.Error.Message);
        }

        [Fact]
        public void OkIf_WithValue_True_ReturnsOkWithValue()
        {
            var result = Result.OkIf(true, 42, "code", "msg");
            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void OkIf_WithValue_False_ReturnsFail()
        {
            var result = Result.OkIf(false, 42, "code", "msg");
            Assert.True(result.IsFailed);
            Assert.Equal("code", result.Error.Code);
        }

        [Fact]
        public void OkIf_WithError_True_ReturnsOk()
        {
            var error = new Error("ERR", "msg");
            var result = Result.OkIf(true, 42, error);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void OkIf_WithError_False_ReturnsFail()
        {
            var error = new Error("ERR", "msg");
            var result = Result.OkIf(false, 42, error);
            Assert.True(result.IsFailed);
            Assert.Equal("ERR", result.Error.Code);
        }

        [Fact]
        public void OkIf_WithTypedError_True_ReturnsOk()
        {
            var error = new TestError { Code = "TERR", Message = "typed" };
            var result = Result.OkIf<int, TestError>(true, 42, error);
            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void OkIf_WithTypedError_False_ReturnsFail()
        {
            var error = new TestError { Code = "TERR", Message = "typed" };
            var result = Result.OkIf<int, TestError>(false, 42, error);
            Assert.True(result.IsFailed);
            Assert.Equal("TERR", result.Error.Code);
        }

        [Fact]
        public void OkIf_VoidResult_True_ReturnsOk()
        {
            var result = Result.OkIf(true, "code", "msg");
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void OkIf_VoidResult_False_ReturnsFail()
        {
            var result = Result.OkIf(false, "code", "msg");
            Assert.True(result.IsFailed);
            Assert.Equal("code", result.Error.Code);
        }
    }

    #endregion

    #region FailIf Tests

    public class FailIfTests
    {
        [Fact]
        public void FailIf_True_ReturnsFail()
        {
            var result = Result.FailIf(true, "code", "msg");
            Assert.True(result.IsFailed);
            Assert.Equal("code", result.Error.Code);
            Assert.Equal("msg", result.Error.Message);
        }

        [Fact]
        public void FailIf_False_ReturnsOk()
        {
            var result = Result.FailIf(false, "code", "msg");
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void FailIf_WithValue_True_ReturnsFail()
        {
            var result = Result.FailIf(true, 42, "code", "msg");
            Assert.True(result.IsFailed);
            Assert.Equal("code", result.Error.Code);
        }

        [Fact]
        public void FailIf_WithValue_False_ReturnsOkWithValue()
        {
            var result = Result.FailIf(false, 42, "code", "msg");
            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void FailIf_WithError_True_ReturnsFail()
        {
            var error = new Error("ERR", "msg");
            var result = Result.FailIf(true, 42, error);
            Assert.True(result.IsFailed);
            Assert.Equal("ERR", result.Error.Code);
        }

        [Fact]
        public void FailIf_WithError_False_ReturnsOk()
        {
            var error = new Error("ERR", "msg");
            var result = Result.FailIf(false, 42, error);
            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void FailIf_WithTypedError_True_ReturnsFail()
        {
            var error = new TestError { Code = "TERR", Message = "typed" };
            var result = Result.FailIf<int, TestError>(true, 42, error);
            Assert.True(result.IsFailed);
            Assert.Equal("TERR", result.Error.Code);
        }

        [Fact]
        public void FailIf_WithTypedError_False_ReturnsOk()
        {
            var error = new TestError { Code = "TERR", Message = "typed" };
            var result = Result.FailIf<int, TestError>(false, 42, error);
            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void FailIf_VoidResult_True_ReturnsFail()
        {
            var result = Result.FailIf(true, "code", "msg");
            Assert.True(result.IsFailed);
        }

        [Fact]
        public void FailIf_VoidResult_False_ReturnsOk()
        {
            var result = Result.FailIf(false, "code", "msg");
            Assert.True(result.IsSuccess);
        }
    }

    #endregion

    #region Real-World Usage Patterns

    public class RealWorldUsagePatterns
    {
        [Fact]
        public void ValidationPattern_OkIf_AgeCheck()
        {
            var age = 20;
            var result = Result.OkIf(
                age >= 18,
                age,
                "Validation.MinorAge",
                "Must be 18 or older");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void ValidationPattern_FailIf_EmptyString()
        {
            var email = "";
            var result = Result.FailIf(
                string.IsNullOrEmpty(email),
                email,
                "Validation.EmptyEmail",
                "Email is required");

            Assert.True(result.IsFailed);
            Assert.Equal("Validation.EmptyEmail", result.Error.Code);
        }

        [Fact]
        public void ValidationChain_MultipleEnsures()
        {
            var email = "not-an-email";
            var result = Result.Ok(email)
                .Ensure(e => !string.IsNullOrEmpty(e), new Error("EMPTY", "Required"))
                .Ensure(e => e.Contains("@"), new Error("FORMAT", "Invalid format"))
                .Ensure(e => e.Contains("."), new Error("DOMAIN", "Missing domain"));

            Assert.True(result.IsFailed);
            Assert.Equal("FORMAT", result.Error.Code);
        }

        [Fact]
        public void RailwayPattern_DataProcessing()
        {
            var result = Result.Ok("  42  ")
                .Map(s => s.Trim())
                .Bind(s => int.TryParse(s, out var n) ? Result.Ok(n) : Result.Fail<int>("PARSE", "Not a number"))
                .Bind(n => n > 0 ? Result.Ok(n * 2) : Result.Fail<int>("NEGATIVE", "Must be positive"));

            Assert.True(result.IsSuccess);
            Assert.Equal(84, result.Value);
        }

        [Fact]
        public void PatternMatching_DisplayMessage()
        {
            var result = Result.Ok(100);
            var message = result.Match(
                v => $"Success: {v} items processed",
                e => $"Error {e.Code}: {e.Message}");
            Assert.Equal("Success: 100 items processed", message);
        }

        [Fact]
        public void Else_DefaultValue()
        {
            var result = Result.Fail<int>("ERR", "msg");
            var value = result.Else(-1);
            Assert.Equal(-1, value);
        }
    }

    #endregion
}
