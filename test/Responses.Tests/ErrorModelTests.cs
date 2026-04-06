using System;
using System.Collections.Generic;
using Xunit;

namespace Responses.Tests;

public class ErrorModelTests
{
    #region ErrorType Tests

    public class ErrorTypeTests
    {
        [Fact]
        public void ErrorType_HasAllExpectedValues()
        {
            Assert.True(Enum.IsDefined(typeof(ErrorType), ErrorType.Unknown));
            Assert.True(Enum.IsDefined(typeof(ErrorType), ErrorType.Validation));
            Assert.True(Enum.IsDefined(typeof(ErrorType), ErrorType.NotFound));
            Assert.True(Enum.IsDefined(typeof(ErrorType), ErrorType.Conflict));
            Assert.True(Enum.IsDefined(typeof(ErrorType), ErrorType.Unauthorized));
            Assert.True(Enum.IsDefined(typeof(ErrorType), ErrorType.Forbidden));
            Assert.True(Enum.IsDefined(typeof(ErrorType), ErrorType.ServerError));
            Assert.True(Enum.IsDefined(typeof(ErrorType), ErrorType.Timeout));
            Assert.True(Enum.IsDefined(typeof(ErrorType), ErrorType.Cancelled));
            Assert.True(Enum.IsDefined(typeof(ErrorType), ErrorType.InternalError));
        }

        [Fact]
        public void ErrorType_DefaultIsUnknown()
        {
            Assert.Equal(ErrorType.Unknown, default(ErrorType));
        }
    }

    #endregion

    #region Error Struct Tests

    public class ErrorStructTests
    {
        [Fact]
        public void Error_CreatesWithCodeAndMessage()
        {
            var error = new Error("ERR001", "Something went wrong");
            Assert.Equal("ERR001", error.Code);
            Assert.Equal("Something went wrong", error.Message);
        }

        [Fact]
        public void Error_CreatesWithDefaultType()
        {
            var error = new Error("ERR001", "msg");
            Assert.Equal(ErrorType.Unknown, error.Type);
        }

        [Fact]
        public void Error_CreatesWithType()
        {
            var error = new Error("ERR001", "msg", ErrorType.Validation);
            Assert.Equal(ErrorType.Validation, error.Type);
        }

        [Fact]
        public void Error_CreatesWithMetadata()
        {
            var metadata = new Dictionary<string, string> { { "key", "value" } };
            var error = new Error("ERR001", "msg", ErrorType.Validation, metadata);
            Assert.Equal("value", error.Metadata["key"]);
        }

        [Fact]
        public void Error_DefaultConstructor_CreatesWithEmptyValues()
        {
            var error = new Error();
            Assert.Equal(string.Empty, error.Code);
            Assert.Equal(string.Empty, error.Message);
            Assert.Equal(ErrorType.Unknown, error.Type);
        }

        [Fact]
        public void Error_NullCode_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Error(null!, "msg"));
        }

        [Fact]
        public void Error_EmptyCode_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Error("", "msg"));
        }

        [Fact]
        public void Error_NullMessage_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Error("code", null!));
        }

        [Fact]
        public void Error_ToString_IncludesTypeAndCode()
        {
            var error = new Error("ERR", "msg", ErrorType.Validation);
            var str = error.ToString();
            Assert.Contains("Validation", str);
            Assert.Contains("ERR", str);
        }

        [Fact]
        public void Error_ImplementsIError()
        {
            IError error = new Error("ERR", "msg", ErrorType.Validation);
            Assert.Equal("ERR", error.Code);
            Assert.Equal("msg", error.Message);
            Assert.Equal(ErrorType.Validation, error.Type);
        }
    }

    #endregion

    #region Error Factory Methods Tests

    public class ErrorFactoryTests
    {
        [Fact]
        public void Error_ValidationFactory_CreatesValidationType()
        {
            var error = Error.Validation("VAL001", "Invalid input");
            Assert.Equal(ErrorType.Validation, error.Type);
            Assert.Equal("VAL001", error.Code);
        }

        [Fact]
        public void Error_NotFoundFactory_CreatesNotFoundType()
        {
            var error = Error.NotFound("NF001", "Resource not found");
            Assert.Equal(ErrorType.NotFound, error.Type);
        }

        [Fact]
        public void Error_ConflictFactory_CreatesConflictType()
        {
            var error = Error.Conflict("CON001", "Duplicate resource");
            Assert.Equal(ErrorType.Conflict, error.Type);
        }

        [Fact]
        public void Error_UnauthorizedFactory_CreatesUnauthorizedType()
        {
            var error = Error.Unauthorized("UA001", "Not authenticated");
            Assert.Equal(ErrorType.Unauthorized, error.Type);
        }

        [Fact]
        public void Error_ForbiddenFactory_CreatesForbiddenType()
        {
            var error = Error.Forbidden("FB001", "Access denied");
            Assert.Equal(ErrorType.Forbidden, error.Type);
        }

        [Fact]
        public void Error_ServerFactory_CreatesServerErrorType()
        {
            var error = Error.Server("SVR001", "Internal error");
            Assert.Equal(ErrorType.ServerError, error.Type);
        }

        [Fact]
        public void Error_TimeoutFactory_CreatesTimeoutType()
        {
            var error = Error.Timeout("TO001", "Request timed out");
            Assert.Equal(ErrorType.Timeout, error.Type);
        }

        [Fact]
        public void Error_CancelledFactory_CreatesCancelledType()
        {
            var error = Error.Cancelled("CAN001", "Operation cancelled");
            Assert.Equal(ErrorType.Cancelled, error.Type);
        }

        [Fact]
        public void Error_Factory_WithMetadata()
        {
            var metadata = new Dictionary<string, string> { { "field", "email" } };
            var error = Error.Validation("VAL001", "Invalid email", metadata);
            Assert.Equal("email", error.Metadata["field"]);
        }
    }

    #endregion

    #region ErrorCollection Tests

    public class ErrorCollectionTests
    {
        [Fact]
        public void ErrorCollection_CreatesWithMultipleErrors()
        {
            var errors = new ErrorCollection(
                Error.Validation("VAL1", "Invalid name"),
                Error.Validation("VAL2", "Invalid email")
            );
            Assert.Equal(2, errors.Count);
        }

        [Fact]
        public void ErrorCollection_Indexer_ReturnsCorrectError()
        {
            var errors = new ErrorCollection(
                Error.Validation("VAL1", "Invalid name"),
                Error.Validation("VAL2", "Invalid email")
            );
            Assert.Equal("VAL1", errors[0].Code);
            Assert.Equal("VAL2", errors[1].Code);
        }

        [Fact]
        public void ErrorCollection_Enumerable_Works()
        {
            var errors = new ErrorCollection(
                Error.Validation("VAL1", "Invalid name"),
                Error.Validation("VAL2", "Invalid email")
            );
            var count = 0;
            foreach (var error in errors)
            {
                count++;
                Assert.NotNull(error.Code);
            }
            Assert.Equal(2, count);
        }

        [Fact]
        public void ErrorCollection_Empty_HasZeroCount()
        {
            Assert.Empty(ErrorCollection.Empty);
        }

        [Fact]
        public void ErrorCollection_CreatesFromEnumerable()
        {
            var list = new List<IError>
            {
                Error.Validation("VAL1", "Invalid"),
                Error.NotFound("NF1", "Missing")
            };
            var errors = new ErrorCollection(list);
            Assert.Equal(2, errors.Count);
        }
    }

    #endregion

    #region Result Multi-Error Tests

    public class ResultMultiErrorTests
    {
        [Fact]
        public void Result_FailWithMultipleErrors_HasErrorsCollection()
        {
            var errors = new IError[]
            {
                Error.Validation("VAL1", "Invalid name"),
                Error.Validation("VAL2", "Invalid email")
            };
            var result = Result.Fail(errors);
            Assert.True(result.IsFailed);
            Assert.Equal(2, result.Errors.Count);
        }

        [Fact]
        public void Result_FailWithMultipleErrors_ErrorCountIsCorrect()
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
        public void Result_Ok_HasEmptyErrors()
        {
            var result = Result.Ok();
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Result_FailWithSingleError_HasErrorsCollectionWithOneItem()
        {
            var result = Result.Fail("ERR", "msg");
            Assert.Single(result.Errors);
            Assert.Equal("ERR", result.Errors[0].Code);
        }

        [Fact]
        public void Result_Error_Property_ReturnsFirstError()
        {
            var errors = new IError[]
            {
                Error.Validation("VAL1", "Invalid name"),
                Error.Validation("VAL2", "Invalid email")
            };
            var result = Result.Fail(errors);
            Assert.Equal("VAL1", result.Error.Code);
        }

        [Fact]
        public void ResultOfT_FailWithMultipleErrors_HasErrorsCollection()
        {
            var errors = new IError[]
            {
                Error.Validation("VAL1", "Invalid"),
                Error.Validation("VAL2", "Invalid email")
            };
            var result = Result.Fail<int>(errors);
            Assert.True(result.IsFailed);
            Assert.Equal(2, result.Errors.Count);
        }

        [Fact]
        public void ResultOfT_Ok_HasEmptyErrors()
        {
            var result = Result.Ok(42);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ResultOfTValueTError_FailWithMultipleErrors_HasErrorsCollection()
        {
            var errors = new IError[]
            {
                Error.Validation("VAL1", "Invalid"),
                Error.Validation("VAL2", "Invalid email")
            };
            var result = Result.Fail<int, Error>(errors);
            Assert.True(result.IsFailed);
            Assert.Equal(2, result.Errors.Count);
        }

        [Fact]
        public void ResultOfTValueTError_Ok_HasEmptyErrors()
        {
            var result = Result.Ok<int, Error>(42);
            Assert.Empty(result.Errors);
        }
    }

    #endregion
}
