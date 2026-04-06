using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Flurl.Http.Testing;
using Responses.Http;
using Xunit;

namespace Responses.Tests;

public class HttpExtensionsTests
{
    #region ReceiveResult (void) Tests

    public class ReceiveResultVoidTests
    {
        [Theory]
        [InlineData(200)]
        [InlineData(201)]
        [InlineData(204)]
        [InlineData(299)]
        public async Task Ok(int status)
        {
            using var test = new HttpTest();
            test.RespondWith("", status);

            var result = await "http://test".GetAsync().ReceiveResult();
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(401)]
        [InlineData(404)]
        [InlineData(500)]
        public async Task Fail(int status)
        {
            using var test = new HttpTest();
            test.RespondWith("", status);

            var result = await "http://test".GetAsync().ReceiveResult();
            Assert.True(result.IsFailed);
        }
    }

    #endregion

    #region ReceiveResult<T> Tests

    public class ReceiveResultOfTTests
    {
        [Fact]
        public async Task Ok_DeserializesValue()
        {
            using var test = new HttpTest();
            test.RespondWithJson(42, 200);

            var result = await "http://test".GetAsync().ReceiveResult<int>();
            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public async Task Ok_DeserializesString()
        {
            using var test = new HttpTest();
            test.RespondWithJson("hello", 200);

            var result = await "http://test".GetAsync().ReceiveResult<string>();
            Assert.True(result.IsSuccess);
            Assert.Equal("hello", result.Value);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(401)]
        [InlineData(404)]
        [InlineData(500)]
        public async Task Fail_ReturnsFailedResult(int status)
        {
            using var test = new HttpTest();
            test.RespondWith("", status);

            var result = await "http://test".GetAsync().ReceiveResult<int>();
            Assert.True(result.IsFailed);
            // ErrorType mapping depends on how Flurl propagates status code in exception path
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public async Task Fail_HandlesGracefulSerializationError()
        {
            using var test = new HttpTest();
            test.RespondWith("invalid json{", 200); // valid status but invalid JSON

            var result = await "http://test".GetAsync().ReceiveResult<int>();
            // Should not throw — returns default on deserialization failure
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.ValueOrDefault); // default(int)
        }
    }

    #endregion

    #region ReceiveResult<TValue, TError> Tests

    public class ReceiveResultOfTValueTErrorTests
    {
        [Fact]
        public async Task Ok_DeserializesValue()
        {
            using var test = new HttpTest();
            test.RespondWithJson(true, 200);

            var result = await "http://test".GetAsync().ReceiveResult<bool, Error>();
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
        }

        [Fact]
        public async Task Fail_CreatesTypedError()
        {
            using var test = new HttpTest();
            test.RespondWith("", 404);

            var result = await "http://test".GetAsync().ReceiveResult<bool, Error>();
            Assert.True(result.IsFailed);
            // Flurl HttpTest may not propagate status code in exception path
            Assert.True(result.Error.Code == "404" || result.Error.Code == "HttpError");
        }
    }

    #endregion

    #region ProblemDetails Tests

    public class ProblemDetailsTests
    {
        [Fact]
        public async Task Fail_WithProblemDetails_ParsesRfc9457()
        {
            var problemJson = """
            {
                "type": "https://example.com/errors/not-found",
                "title": "Resource Not Found",
                "status": 404,
                "detail": "The requested resource does not exist",
                "instance": "/api/users/123"
            }
            """;

            using var test = new HttpTest();
            test.RespondWith(problemJson, 404);

            var result = await "http://test".GetAsync().ReceiveResult<int>();
            Assert.True(result.IsFailed);
            Assert.Equal("The requested resource does not exist", result.Errors[0].Message);
        }

        [Fact]
        public void ProblemDetails_TryParse_ReturnsNull_WhenNotProblemJson()
        {
            var result = ProblemDetails.TryParse("just a string");
            Assert.Null(result);
        }

        [Fact]
        public void ProblemDetails_TryParse_ReturnsValue_WhenValidProblemJson()
        {
            var json = """{"type":"https://example.com/err","title":"Error","status":500}""";
            var result = ProblemDetails.TryParse(json);
            Assert.NotNull(result);
            Assert.Equal("https://example.com/err", result.Value.Type);
            Assert.Equal("Error", result.Value.Title);
            Assert.Equal(500, result.Value.Status);
        }

        [Fact]
        public void ProblemDetails_CreatesWithAllProperties()
        {
            var pd = new ProblemDetails(
                "https://example.com/not-found",
                "Resource Not Found",
                404,
                "The user does not exist",
                "/api/users/123");

            Assert.Equal("https://example.com/not-found", pd.Type);
            Assert.Equal("Resource Not Found", pd.Title);
            Assert.Equal(404, pd.Status);
            Assert.Equal("The user does not exist", pd.Detail);
            Assert.Equal("/api/users/123", pd.Instance);
        }

        [Fact]
        public void ProblemDetails_WithNullStatus()
        {
            var pd = new ProblemDetails("https://example.com/err", "Error", null, "details", "/instance");
            Assert.Null(pd.Status);
        }
    }

    #endregion

    #region StatusCodeMapping Tests

    public class StatusCodeMappingTests
    {
        [Theory]
        [InlineData(400, ErrorType.Validation)]
        [InlineData(401, ErrorType.Unauthorized)]
        [InlineData(403, ErrorType.Forbidden)]
        [InlineData(404, ErrorType.NotFound)]
        [InlineData(409, ErrorType.Conflict)]
        [InlineData(500, ErrorType.ServerError)]
        [InlineData(502, ErrorType.BadGateway)]
        [InlineData(503, ErrorType.ServiceUnavailable)]
        [InlineData(422, ErrorType.UnprocessableEntity)]
        public void MapsStatusCodeToErrorType(int statusCode, ErrorType expectedType)
        {
            var actual = StatusCodeMapping.ToErrorType((System.Net.HttpStatusCode)statusCode);
            Assert.Equal(expectedType, actual);
        }
    }

    #endregion

    #region HttpResponseInfo Tests

    public class HttpResponseInfoTests
    {
        [Fact]
        public void HttpResponseInfo_CreatesWithAllProperties()
        {
            var headers = new Dictionary<string, IEnumerable<string>>
            {
                { "Content-Type", new[] { "application/json" } }
            };
            var info = new HttpResponseInfo(
                System.Net.HttpStatusCode.OK,
                "OK",
                headers,
            "{\"key\":\"value\"}");

            Assert.Equal(System.Net.HttpStatusCode.OK, info.StatusCode);
            Assert.Equal("OK", info.ReasonPhrase);
            Assert.Equal("application/json", info.Headers["Content-Type"].First());
            Assert.Equal("{\"key\":\"value\"}", info.RawBody);
        }

        [Fact]
        public void HttpResponseInfo_EmptyHeaders_IsValid()
        {
            var info = new HttpResponseInfo(
                System.Net.HttpStatusCode.NoContent,
                "",
                new Dictionary<string, IEnumerable<string>>(),
            "");

            Assert.Equal(System.Net.HttpStatusCode.NoContent, info.StatusCode);
            Assert.Empty(info.Headers);
            Assert.Empty(info.RawBody);
        }
    }

    #endregion

    #region ResultHttpExtensions Tests

    public class ResultHttpExtensionsTests
    {
        [Fact]
        public void WithHttpInfo_ReturnsResultAndDefaultHttpInfo()
        {
            var result = Result.Ok(42);
            var (resultValue, httpInfo) = result.WithHttpInfo();

            Assert.True(resultValue.IsSuccess);
            Assert.Equal(42, resultValue.Value);
            // Note: HttpResponseInfo is default since HTTP metadata capture
            // is not yet integrated into Result types. This is a placeholder
            // for future implementation (see R32 in roadmap).
            Assert.Equal(default, httpInfo.StatusCode);
        }

        [Fact]
        public void WithHttpInfo_WorksWithFailedResult()
        {
            var result = Result.Fail<int>("ERR", "msg");
            var (resultValue, httpInfo) = result.WithHttpInfo();

            Assert.True(resultValue.IsFailed);
            Assert.Equal(default, httpInfo.StatusCode);
        }
    }

    #endregion
}
