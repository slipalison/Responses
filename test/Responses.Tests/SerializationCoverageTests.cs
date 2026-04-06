using System;
using System.Collections.Generic;
using System.Text.Json;
using Responses.Serialization;
using Xunit;

namespace Responses.Tests;

/// <summary>
/// Comprehensive tests for Serialization classes coverage.
/// Covers: ResultDto, ResultDto&lt;T&gt;, ResultDto&lt;TValue,TError&gt;, ErrorDto,
/// ResultJsonContext. Uses the DTO pattern — no custom converters needed.
/// </summary>
public class SerializationCoverageTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    #region ResultDto Edge Cases

    public class ResultDtoEdgeCases
    {
        [Fact]
        public void ResultDto_NullErrors_UsesEmptyArray()
        {
            var dto = new ResultDto(true, null!);
            Assert.NotNull(dto.Errors);
            Assert.Empty(dto.Errors);
        }

        [Fact]
        public void ResultDto_EmptyErrors_ToResult_ReturnsOk()
        {
            var dto = new ResultDto(true, Array.Empty<ErrorDto>());
            var result = dto.ToResult();
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void ResultDto_NoErrors_IsSuccessfulFalse_ReturnsFailWithDefaultError()
        {
            var dto = new ResultDto(false, Array.Empty<ErrorDto>());
            var result = dto.ToResult();
            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal("Unknown", result.Errors[0].Code);
        }

        [Fact]
        public void ResultDto_FromResult_Success_HasEmptyErrors()
        {
            var result = Result.Ok();
            var dto = ResultDto.FromResult(result);
            Assert.True(dto.IsSuccessful);
            Assert.Empty(dto.Errors);
        }
    }

    #endregion

    #region ResultDto<T> Edge Cases

    public class ResultDtoOfTEdgeCases
    {
        [Fact]
        public void ResultDtoOfT_NullErrors_UsesEmptyArray()
        {
            var dto = new ResultDto<int>(true, 42, null!);
            Assert.NotNull(dto.Errors);
            Assert.Empty(dto.Errors);
        }

        [Fact]
        public void ResultDtoOfT_NullValue_ToResult_ReturnsOkWithDefault()
        {
            var dto = new ResultDto<string?>(true, null, Array.Empty<ErrorDto>());
            var result = dto.ToResult();
            Assert.True(result.IsSuccess);
            Assert.Null(result.ValueOrDefault);
        }

        [Fact]
        public void ResultDtoOfT_Fail_NoErrors_ReturnsFailWithDefaultError()
        {
            var dto = new ResultDto<int>(false, 0, Array.Empty<ErrorDto>());
            var result = dto.ToResult();
            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
            Assert.Equal("Unknown", result.Errors[0].Code);
        }

        [Fact]
        public void ResultDtoOfT_ReferenceTypeRoundTrip()
        {
            var result = Result.Ok("hello world");
            var dto = ResultDto<string>.FromResult(result);
            var json = JsonSerializer.Serialize(dto, Options);
            var dtoBack = JsonSerializer.Deserialize<ResultDto<string>>(json, Options)!;
            var resultBack = dtoBack.ToResult();

            Assert.True(resultBack.IsSuccess);
            Assert.Equal("hello world", resultBack.Value);
        }

        [Fact]
        public void ResultDtoOfT_WithComplexValue()
        {
            var dict = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
            var result = Result.Ok(dict);
            var dto = ResultDto<Dictionary<string, int>>.FromResult(result);
            var json = JsonSerializer.Serialize(dto, Options);
            var dtoBack = JsonSerializer.Deserialize<ResultDto<Dictionary<string, int>>>(json, Options)!;
            var resultBack = dtoBack.ToResult();

            Assert.True(resultBack.IsSuccess);
            Assert.Equal(2, resultBack.ValueOrDefault!["b"]);
        }
    }

    #endregion

    #region ResultDto<TValue, TError> Coverage

    public class ResultDtoOfTValueTErrorTests
    {
        [Fact]
        public void ResultDtoOfTValueTError_Success_RoundTrip()
        {
            var result = Result.Ok<int, Error>(42);
            var dto = ResultDto<int, Error>.FromResult(result);
            var json = JsonSerializer.Serialize(dto, Options);
            var dtoBack = JsonSerializer.Deserialize<ResultDto<int, Error>>(json, Options)!;
            var resultBack = dtoBack.ToResult();

            Assert.True(resultBack.IsSuccess);
            Assert.Equal(42, resultBack.Value);
        }

        [Fact]
        public void ResultDtoOfTValueTError_Fail_RoundTrip()
        {
            var error = Error.Validation("VAL", "Invalid input");
            var result = Result.Fail<int, Error>(error);
            var dto = ResultDto<int, Error>.FromResult(result);
            var json = JsonSerializer.Serialize(dto, Options);
            var dtoBack = JsonSerializer.Deserialize<ResultDto<int, Error>>(json, Options)!;
            var resultBack = dtoBack.ToResult();

            Assert.True(resultBack.IsFailed);
            Assert.Equal(ErrorType.Validation, resultBack.Errors[0].Type);
            Assert.Equal("VAL", resultBack.Errors[0].Code);
        }

        [Fact]
        public void ResultDtoOfTValueTError_FailWithMultipleErrors_RoundTrip()
        {
            var errors = new IError[]
            {
                Error.Validation("VAL1", "Invalid name"),
                Error.Validation("VAL2", "Invalid email")
            };
            var result = Result.Fail<int, Error>(errors);
            var dto = ResultDto<int, Error>.FromResult(result);
            var json = JsonSerializer.Serialize(dto, Options);
            var dtoBack = JsonSerializer.Deserialize<ResultDto<int, Error>>(json, Options)!;
            var resultBack = dtoBack.ToResult();

            Assert.True(resultBack.IsFailed);
            Assert.Equal(2, resultBack.Errors.Count);
        }

        [Fact]
        public void ResultDtoOfTValueTError_NullErrors_UsesEmptyArray()
        {
            var dto = new ResultDto<int, Error>(true, 42, null!);
            Assert.NotNull(dto.Errors);
            Assert.Empty(dto.Errors);
        }

        [Fact]
        public void ResultDtoOfTValueTError_NoErrors_IsSuccessfulFalse_ReturnsFail()
        {
            var dto = new ResultDto<int, Error>(false, 0, Array.Empty<ErrorDto>());
            var result = dto.ToResult();
            Assert.True(result.IsFailed);
            Assert.Single(result.Errors);
        }
    }

    #endregion

    #region ErrorDto Edge Cases

    public class ErrorDtoEdgeCases
    {
        [Fact]
        public void ErrorDto_NullConstructorValues_HandlesGracefully()
        {
            var dto = new ErrorDto(null!, null!, ErrorType.Unknown, null!, null!, null);
            Assert.Equal(string.Empty, dto.Code);
            Assert.Equal(string.Empty, dto.Message);
            Assert.Equal(string.Empty, dto.Layer);
            Assert.Equal(string.Empty, dto.ApplicationName);
            Assert.NotNull(dto.Metadata);
            Assert.Empty(dto.Metadata);
        }

        [Fact]
        public void ErrorDto_FromError_WithEmptyMetadata()
        {
            var error = new Error("ERR", "msg");
            var dto = ErrorDto.FromError(error);
            Assert.NotNull(dto.Metadata);
            Assert.Empty(dto.Metadata);
        }

        [Fact]
        public void ErrorDto_ToError_PreservesAllFields()
        {
            var metadata = new Dictionary<string, string> { { "key", "value" } };
            var dto = new ErrorDto("ERR", "message", ErrorType.NotFound, "Layer", "App", metadata);
            var error = dto.ToError();

            Assert.Equal("ERR", error.Code);
            Assert.Equal("message", error.Message);
            Assert.Equal(ErrorType.NotFound, error.Type);
            Assert.Equal("value", error.Metadata["key"]);
        }

        [Fact]
        public void ErrorDto_FromTestError_RoundTrip()
        {
            var testError = new SerializationTest.TestError
            {
                Code = "TEST",
                Message = "Test error",
                Type = ErrorType.ServerError,
                Layer = "TestLayer",
                ApplicationName = "TestApp"
            };
            var dto = ErrorDto.FromError(testError);
            var error = dto.ToError();

            Assert.Equal("TEST", error.Code);
            Assert.Equal("Test error", error.Message);
            Assert.Equal(ErrorType.ServerError, error.Type);
        }
    }

    #endregion

    #region ResultJsonContext Coverage

    public class ResultJsonContextTests
    {
        [Fact]
        public void DefaultOptions_IsNotNull()
        {
            Assert.NotNull(ResultJsonContext.DefaultOptions);
        }

        [Fact]
        public void DefaultOptions_CanBeUsedForSerialization()
        {
            var result = Result.Ok(42);
            var json = JsonSerializer.Serialize(result, ResultJsonContext.DefaultOptions);
            Assert.NotNull(json);
            Assert.Contains("isSuccessful", json);
        }

        [Fact]
        public void SerializeWithSourceGenerator_Result()
        {
            var result = Result.Ok();
            var json = JsonSerializer.Serialize(result, ResultJsonContext.Default.Result);
            Assert.Contains("isSuccessful", json);
            Assert.Contains("true", json);
        }

        [Fact]
        public void SerializeWithSourceGenerator_ResultOfT()
        {
            var result = Result.Ok(42);
            var json = JsonSerializer.Serialize(result, ResultJsonContext.Default.ResultInt32);
            Assert.Contains("isSuccessful", json);
            Assert.Contains("42", json);
        }

        [Fact]
        public void SerializeWithSourceGenerator_Error()
        {
            var error = Error.Validation("ERR", "msg");
            var json = JsonSerializer.Serialize(error, ResultJsonContext.Default.Error);
            Assert.Contains("ERR", json);
            // ErrorType is serialized as numeric by source-gen
            Assert.Contains("type", json);
        }
    }

    #endregion

    #region Integration: Serialize-only with Source Generator

    public class SourceGeneratorIntegrationTests
    {
        [Fact]
        public void SerializeResult_ProducesValidJson()
        {
            var original = Result.Ok();
            var json = JsonSerializer.Serialize(original, ResultJsonContext.Default.Result);
            Assert.NotNull(json);
        }

        [Fact]
        public void SerializeResultOfT_ProducesValidJson()
        {
            var original = Result.Ok(42);
            var json = JsonSerializer.Serialize(original, ResultJsonContext.Default.ResultInt32);
            Assert.NotNull(json);
            Assert.Contains("isSuccessful", json);
        }

        [Fact]
        public void SerializeResultOfString_ProducesValidJson()
        {
            var original = Result.Ok("hello");
            var json = JsonSerializer.Serialize(original, ResultJsonContext.Default.ResultString);
            Assert.NotNull(json);
            Assert.Contains("hello", json);
        }

        [Fact]
        public void SerializeFailResult_ProducesErrorJson()
        {
            var original = Result.Fail<int>("ERR001", "Something failed");
            var json = JsonSerializer.Serialize(original, ResultJsonContext.Default.ResultInt32);
            Assert.Contains("isSuccessful", json);
            Assert.Contains("false", json);
            Assert.Contains("errors", json);
        }
    }

    #endregion
}
