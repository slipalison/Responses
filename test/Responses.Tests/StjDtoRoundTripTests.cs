using System;
using System.Collections.Generic;
using System.Text.Json;
using Responses.Serialization;
using Xunit;

namespace Responses.Tests;

/// <summary>
/// Tests for STJ round-trip serialization using DTO types.
/// This solves the deserialization problem with readonly structs.
/// </summary>
public class StjDtoRoundTripTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    #region ResultDto (void) Tests

    public class ResultDtoTests
    {
        [Fact]
        public void ResultOk_RoundTrip()
        {
            var result = Result.Ok();
            var dto = ResultDto.FromResult(result);
            var json = JsonSerializer.Serialize(dto, Options);
            var dtoBack = JsonSerializer.Deserialize<ResultDto>(json, Options)!;
            var resultBack = dtoBack.ToResult();

            Assert.True(resultBack.IsSuccess);
        }

        [Fact]
        public void ResultFail_RoundTrip()
        {
            var result = Result.Fail("ERR001", "Something went wrong");
            var dto = ResultDto.FromResult(result);
            var json = JsonSerializer.Serialize(dto, Options);
            var dtoBack = JsonSerializer.Deserialize<ResultDto>(json, Options)!;
            var resultBack = dtoBack.ToResult();

            Assert.True(resultBack.IsFailed);
            Assert.Equal("ERR001", resultBack.Errors[0].Code);
            Assert.Equal("Something went wrong", resultBack.Errors[0].Message);
        }
    }

    #endregion

    #region ResultDto<T> Tests

    public class ResultDtoOfTTests
    {
        [Fact]
        public void ResultOfT_Ok_RoundTrip()
        {
            var result = Result.Ok(42);
            var dto = ResultDto<int>.FromResult(result);
            var json = JsonSerializer.Serialize(dto, Options);
            var dtoBack = JsonSerializer.Deserialize<ResultDto<int>>(json, Options)!;
            var resultBack = dtoBack.ToResult();

            Assert.True(resultBack.IsSuccess);
            Assert.Equal(42, resultBack.Value);
        }

        [Fact]
        public void ResultOfT_StringValue_RoundTrip()
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
        public void ResultOfT_Fail_RoundTrip()
        {
            var result = Result.Fail<int>("ERR001", "Something went wrong");
            var dto = ResultDto<int>.FromResult(result);
            var json = JsonSerializer.Serialize(dto, Options);
            var dtoBack = JsonSerializer.Deserialize<ResultDto<int>>(json, Options)!;
            var resultBack = dtoBack.ToResult();

            Assert.True(resultBack.IsFailed);
            Assert.Equal("ERR001", resultBack.Errors[0].Code);
        }

        [Fact]
        public void ResultOfT_FailWithMultipleErrors_RoundTrip()
        {
            var errors = new IError[]
            {
                Error.Validation("VAL1", "Invalid name"),
                Error.Validation("VAL2", "Invalid email")
            };
            var result = Result.Fail<int>(errors);
            var dto = ResultDto<int>.FromResult(result);
            var json = JsonSerializer.Serialize(dto, Options);
            var dtoBack = JsonSerializer.Deserialize<ResultDto<int>>(json, Options)!;
            var resultBack = dtoBack.ToResult();

            Assert.True(resultBack.IsFailed);
            Assert.Equal(2, resultBack.Errors.Count);
            Assert.Equal("VAL1", resultBack.Errors[0].Code);
            Assert.Equal("VAL2", resultBack.Errors[1].Code);
        }
    }

    #endregion

    #region ErrorDto Tests

    public class ErrorDtoTests
    {
        [Fact]
        public void Error_WithTypeAndMetadata_RoundTrip()
        {
            var metadata = new Dictionary<string, string> { { "field", "email" }, { "value", "invalid" } };
            var error = new Error("ERR001", "Invalid email", ErrorType.Validation, metadata);
            var dto = ErrorDto.FromError(error);
            var json = JsonSerializer.Serialize(dto, Options);
            var dtoBack = JsonSerializer.Deserialize<ErrorDto>(json, Options)!;
            var errorBack = dtoBack.ToError();

            Assert.Equal("ERR001", errorBack.Code);
            Assert.Equal("Invalid email", errorBack.Message);
            Assert.Equal(ErrorType.Validation, errorBack.Type);
            Assert.Equal("email", errorBack.Metadata["field"]);
        }

        [Fact]
        public void Error_FactoryMethods_RoundTrip()
        {
            var error = Error.NotFound("NF001", "Resource not found");
            var dto = ErrorDto.FromError(error);
            var json = JsonSerializer.Serialize(dto, Options);
            var dtoBack = JsonSerializer.Deserialize<ErrorDto>(json, Options)!;
            var errorBack = dtoBack.ToError();

            Assert.Equal(ErrorType.NotFound, errorBack.Type);
            Assert.Equal("NF001", errorBack.Code);
        }
    }

    #endregion
}
