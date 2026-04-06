using System;
using System.Collections.Generic;
using System.Text.Json;
using Responses.Serialization;
using Xunit;

namespace Responses.Tests;

public class StjSerializationTests
{
    private static readonly JsonSerializerOptions Options = ResultJsonContext.DefaultOptions;

    #region Error Serialization

    public class ErrorSerialization
    {
        [Fact]
        public void Error_SerializeAndDeserialize_RoundTrip()
        {
            var error = new Error("ERR001", "Something went wrong", ErrorType.Validation);
            var json = JsonSerializer.Serialize(error, Options);
            var back = JsonSerializer.Deserialize<IError>(json, Options)!;
            Assert.Equal("ERR001", back.Code);
            Assert.Equal("Something went wrong", back.Message);
            Assert.Equal(ErrorType.Validation, back.Type);
        }

        [Fact]
        public void Error_Serialize_WithMetadata_IncludesMetadataInJson()
        {
            var metadata = new Dictionary<string, string> { { "field", "email" }, { "value", "invalid" } };
            var error = new Error("ERR001", "Invalid email", ErrorType.Validation, metadata);
            var json = JsonSerializer.Serialize(error, Options);
            Assert.Contains("metadata", json);
            var back = JsonSerializer.Deserialize<IError>(json, Options)!;
            Assert.Equal("email", back.Metadata["field"]);
        }

        [Fact]
        public void Error_Deserialize_WithoutMetadata_HandlesGracefully()
        {
            var json = """{"code":"ERR","message":"msg","type":"Unknown"}""";
            var back = JsonSerializer.Deserialize<IError>(json, Options)!;
            Assert.Equal("ERR", back.Code);
            Assert.Equal(ErrorType.Unknown, back.Type);
        }

        [Fact]
        public void Error_Serialize_FactoryMethods_PreserveType()
        {
            var error = Error.NotFound("NF001", "Resource not found");
            var json = JsonSerializer.Serialize(error, Options);
            var back = JsonSerializer.Deserialize<IError>(json, Options)!;
            Assert.Equal(ErrorType.NotFound, back.Type);
        }
    }

    #endregion

    #region ErrorCollection Serialization

    public class ErrorCollectionSerialization
    {
        [Fact]
        public void ErrorCollection_SerializeAsArray()
        {
            var errors = new ErrorCollection(
                Error.Validation("VAL1", "Invalid name"),
                Error.Validation("VAL2", "Invalid email")
            );
            var json = JsonSerializer.Serialize(errors, Options);
            Assert.StartsWith("[", json);
            Assert.Contains("VAL1", json);
            Assert.Contains("VAL2", json);
        }

        [Fact]
        public void ErrorCollection_DeserializesCorrectly()
        {
            var errors = new ErrorCollection(
                Error.Validation("VAL1", "Invalid name"),
                Error.Validation("VAL2", "Invalid email")
            );
            var json = JsonSerializer.Serialize(errors, Options);
            var back = JsonSerializer.Deserialize<ErrorCollection>(json, Options)!;
            Assert.Equal(2, back.Count);
        }
    }

    #endregion

    #region Result Serialization

    public class ResultSerialization
    {
        [Fact]
        public void Result_SerializeOk_RoundTrip()
        {
            var result = Result.Ok();
            var json = JsonSerializer.Serialize(result, typeof(Result), Options);
            var back = (Result)JsonSerializer.Deserialize(json, typeof(Result))!;
            Assert.True(back.IsSuccess);
        }

        [Fact]
        public void Result_SerializeFail_RoundTrip()
        {
            var result = Result.Fail("ERR001", "Something went wrong");
            var json = JsonSerializer.Serialize(result, typeof(Result), Options);
            var back = (Result)JsonSerializer.Deserialize(json, typeof(Result))!;
            Assert.True(back.IsFailed);
            Assert.Equal("ERR001", back.Error.Code);
        }

        [Fact]
        public void ResultOfT_SerializeOk_RoundTrip()
        {
            var result = Result.Ok(42);
            var json = JsonSerializer.Serialize(result, typeof(Result<int>), Options);
            var back = (Result<int>)JsonSerializer.Deserialize(json, typeof(Result<int>))!;
            Assert.True(back.IsSuccess);
            Assert.Equal(42, back.Value);
        }

        [Fact]
        public void ResultOfT_SerializeFail_RoundTrip()
        {
            var result = Result.Fail<int>("ERR001", "Something went wrong");
            var json = JsonSerializer.Serialize(result, typeof(Result<int>), Options);
            var back = (Result<int>)JsonSerializer.Deserialize(json, typeof(Result<int>))!;
            Assert.True(back.IsFailed);
            Assert.Equal("ERR001", back.Error.Code);
        }

        [Fact]
        public void ResultOfT_SerializeWithStringValue()
        {
            var result = Result.Ok("hello world");
            var json = JsonSerializer.Serialize(result, typeof(Result<string>), Options);
            var back = (Result<string>)JsonSerializer.Deserialize(json, typeof(Result<string>))!;
            Assert.True(back.IsSuccess);
            Assert.Equal("hello world", back.Value);
        }
    }

    #endregion

    #region Result<TValue, TError> Serialization

    public class ResultOfTValueTErrorSerialization
    {
        [Fact]
        public void ResultOfTValueTError_SerializeOk_RoundTrip()
        {
            var result = Result.Ok<int, Error>(42);
            var json = JsonSerializer.Serialize(result, typeof(Result<int, Error>), Options);
            var back = (Result<int, Error>)JsonSerializer.Deserialize(json, typeof(Result<int, Error>))!;
            Assert.True(back.IsSuccess);
            Assert.Equal(42, back.Value);
        }

        [Fact]
        public void ResultOfTValueTError_SerializeFail_RoundTrip()
        {
            var error = Error.Validation("VAL001", "Invalid input");
            var result = Result.Fail<int, Error>(error);
            var json = JsonSerializer.Serialize(result, typeof(Result<int, Error>), Options);
            var back = (Result<int, Error>)JsonSerializer.Deserialize(json, typeof(Result<int, Error>))!;
            Assert.True(back.IsFailed);
            Assert.Equal(ErrorType.Validation, back.Error.Type);
        }
    }

    #endregion

    #region Multi-Error Serialization

    public class MultiErrorSerialization
    {
        [Fact]
        public void Result_WithMultipleErrors_SerializesAsArray()
        {
            var errors = new IError[] { Error.Validation("VAL1", "Invalid name"), Error.Validation("VAL2", "Invalid email") };
            var result = Result.Fail(errors);
            var json = JsonSerializer.Serialize(result, typeof(Result), Options);
            Assert.Contains("errors", json);
            Assert.Contains("VAL1", json);
            Assert.Contains("VAL2", json);
        }

        [Fact]
        public void Result_WithMultipleErrors_DeserializesCorrectly()
        {
            var errors = new IError[] { Error.Validation("VAL1", "Invalid name"), Error.Validation("VAL2", "Invalid email") };
            var result = Result.Fail(errors);
            var json = JsonSerializer.Serialize(result, typeof(Result), Options);
            var back = (Result)JsonSerializer.Deserialize(json, typeof(Result))!;
            Assert.True(back.IsFailed);
            Assert.Equal(2, back.Errors.Count);
        }

        [Fact]
        public void ResultOfT_WithMultipleErrors_RoundTrip()
        {
            var errors = new IError[] { Error.Validation("VAL1", "Invalid"), Error.NotFound("NF1", "Missing") };
            var result = Result.Fail<int>(errors);
            var json = JsonSerializer.Serialize(result, typeof(Result<int>), Options);
            var back = (Result<int>)JsonSerializer.Deserialize(json, typeof(Result<int>))!;
            Assert.True(back.IsFailed);
            Assert.Equal(2, back.Errors.Count);
        }
    }

    #endregion
}
