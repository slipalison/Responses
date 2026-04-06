using System;
using System.Net;
using Responses.Http;
using Xunit;

namespace Responses.Tests;

/// <summary>
/// Comprehensive tests for StatusCodeMapping.ToErrorType().
/// Validates that ErrorType values match HTTP status codes where applicable
/// and that fallback behavior is correct for unmapped codes.
/// </summary>
public class StatusCodeMappingTests
{
    #region ErrorType Values Match Status Codes

    public class EnumValueMatchesStatusCode
    {
        [Theory]
        [InlineData(ErrorType.Validation, 400)]
        [InlineData(ErrorType.Unauthorized, 401)]
        [InlineData(ErrorType.Forbidden, 403)]
        [InlineData(ErrorType.NotFound, 404)]
        [InlineData(ErrorType.Timeout, 408)]
        [InlineData(ErrorType.Conflict, 409)]
        [InlineData(ErrorType.Gone, 410)]
        [InlineData(ErrorType.UnprocessableEntity, 422)]
        [InlineData(ErrorType.Locked, 423)]
        [InlineData(ErrorType.FailedDependency, 424)]
        [InlineData(ErrorType.UpgradeRequired, 426)]
        [InlineData(ErrorType.PreconditionRequired, 428)]
        [InlineData(ErrorType.TooManyRequests, 429)]
        [InlineData(ErrorType.UnavailableForLegal, 451)]
        [InlineData(ErrorType.ClientClosed, 499)]
        [InlineData(ErrorType.ServerError, 500)]
        [InlineData(ErrorType.BadGateway, 502)]
        [InlineData(ErrorType.ServiceUnavailable, 503)]
        [InlineData(ErrorType.GatewayTimeout, 504)]
        public void ErrorType_ValueEqualsHttpStatusCode(ErrorType errorType, int expectedCode)
        {
            Assert.Equal(expectedCode, (int)errorType);
        }
    }

    #endregion

    #region StatusCodeMapping → Exact Matches

    public class ExactStatusCodeMappings
    {
        [Theory]
        [InlineData(400, ErrorType.Validation)]
        [InlineData(401, ErrorType.Unauthorized)]
        [InlineData(403, ErrorType.Forbidden)]
        [InlineData(404, ErrorType.NotFound)]
        [InlineData(408, ErrorType.Timeout)]
        [InlineData(409, ErrorType.Conflict)]
        [InlineData(410, ErrorType.Gone)]
        [InlineData(422, ErrorType.UnprocessableEntity)]
        [InlineData(423, ErrorType.Locked)]
        [InlineData(424, ErrorType.FailedDependency)]
        [InlineData(426, ErrorType.UpgradeRequired)]
        [InlineData(428, ErrorType.PreconditionRequired)]
        [InlineData(429, ErrorType.TooManyRequests)]
        [InlineData(451, ErrorType.UnavailableForLegal)]
        [InlineData(499, ErrorType.ClientClosed)]
        [InlineData(500, ErrorType.ServerError)]
        [InlineData(502, ErrorType.BadGateway)]
        [InlineData(503, ErrorType.ServiceUnavailable)]
        [InlineData(504, ErrorType.GatewayTimeout)]
        public void ToErrorType_ExactMatch_ReturnsMatchingErrorType(int code, ErrorType expected)
        {
            var result = StatusCodeMapping.ToErrorType((HttpStatusCode)code);
            Assert.Equal(expected, result);
            // Verify the enum value equals the status code
            Assert.Equal(code, (int)result);
        }
    }

    #endregion

    #region Fallback Behavior

    public class FallbackBehavior
    {
        [Theory]
        [InlineData(402)]  // Payment Required (reserved)
        [InlineData(405)]  // Method Not Allowed
        [InlineData(406)]  // Not Acceptable
        [InlineData(411)]  // Length Required
        [InlineData(413)]  // Content Too Large
        [InlineData(418)]  // I'm a teapot
        [InlineData(421)]  // Misdirected Request
        [InlineData(425)]  // Too Early
        [InlineData(431)]  // Request Header Fields Too Large
        [InlineData(444)]  // Custom (nginx connection closed without response)
        [InlineData(450)]  // Custom
        [InlineData(498)]  // Custom
        public void ToErrorType_4xx_Unmapped_ReturnsValidation(int code)
        {
            Assert.Equal(ErrorType.Validation, StatusCodeMapping.ToErrorType((HttpStatusCode)code));
        }

        [Theory]
        [InlineData(501)]  // Not Implemented
        [InlineData(505)]  // HTTP Version Not Supported
        [InlineData(506)]  // Variant Also Negotiates
        [InlineData(507)]  // Insufficient Storage
        [InlineData(508)]  // Loop Detected
        [InlineData(510)]  // Not Extended
        [InlineData(511)]  // Network Authentication Required
        [InlineData(520)]  // Custom (Cloudflare)
        [InlineData(599)]  // Custom
        public void ToErrorType_5xx_Unmapped_ReturnsServerError(int code)
        {
            Assert.Equal(ErrorType.ServerError, StatusCodeMapping.ToErrorType((HttpStatusCode)code));
        }
    }

    #endregion

    #region Non-Error Codes

    public class NonErrorCodes
    {
        [Theory]
        [InlineData(100)]  // Continue
        [InlineData(101)]  // Switching Protocols
        [InlineData(200)]  // OK
        [InlineData(201)]  // Created
        [InlineData(204)]  // No Content
        [InlineData(301)]  // Moved Permanently
        [InlineData(302)]  // Found
        [InlineData(304)]  // Not Modified
        public void ToErrorType_1xx_2xx_3xx_ReturnsUnknown(int code)
        {
            Assert.Equal(ErrorType.Unknown, StatusCodeMapping.ToErrorType((HttpStatusCode)code));
        }

        [Theory]
        [InlineData(600)]
        [InlineData(999)]
        public void ToErrorType_OutOfRange_ReturnsUnknown(int code)
        {
            Assert.Equal(ErrorType.Unknown, StatusCodeMapping.ToErrorType((HttpStatusCode)code));
        }
    }

    #endregion

    #region Non-HTTP Error Types

    public class NonHttpErrorTypes
    {
        [Fact]
        public void Cancelled_HasDistinctValue()
        {
            Assert.Equal(999, (int)ErrorType.Cancelled);
        }

        [Fact]
        public void InternalError_HasDistinctValue()
        {
            Assert.Equal(998, (int)ErrorType.InternalError);
        }

        [Fact]
        public void Unknown_IsZero()
        {
            Assert.Equal(0, (int)ErrorType.Unknown);
        }
    }

    #endregion
}
