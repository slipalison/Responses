using System.Net;
using Responses.Http;
using Xunit;

namespace Responses.Tests;

/// <summary>
/// Comprehensive tests for StatusCodeMapping.ToErrorType().
/// Covers all standard HTTP status codes (RFC 9110, RFC 6585, RFC 4918, RFC 7725)
/// plus fallback behavior for unmapped and custom codes.
/// </summary>
public class StatusCodeMappingTests
{
    #region 4xx Client Errors — Specific Mappings

    public class FourXxSpecificMappings
    {
        [Theory]
        [InlineData(400, ErrorType.Validation)]
        [InlineData(405, ErrorType.Validation)]
        [InlineData(406, ErrorType.Validation)]
        [InlineData(411, ErrorType.Validation)]
        [InlineData(412, ErrorType.Validation)]
        [InlineData(413, ErrorType.Validation)]
        [InlineData(414, ErrorType.Validation)]
        [InlineData(415, ErrorType.Validation)]
        [InlineData(416, ErrorType.Validation)]
        [InlineData(417, ErrorType.Validation)]
        [InlineData(421, ErrorType.Validation)]
        [InlineData(425, ErrorType.Validation)]
        [InlineData(431, ErrorType.Validation)]
        public void ToErrorType_4xx_MapsToValidation(int code, ErrorType expected)
        {
            Assert.Equal(expected, StatusCodeMapping.ToErrorType((HttpStatusCode)code));
        }

        [Theory]
        [InlineData(401, ErrorType.Unauthorized)]
        [InlineData(407, ErrorType.Unauthorized)]
        public void ToErrorType_4xx_MapsToUnauthorized(int code, ErrorType expected)
        {
            Assert.Equal(expected, StatusCodeMapping.ToErrorType((HttpStatusCode)code));
        }

        [Fact]
        public void ToErrorType_403_ReturnsForbidden()
        {
            Assert.Equal(ErrorType.Forbidden, StatusCodeMapping.ToErrorType(HttpStatusCode.Forbidden));
        }

        [Theory]
        [InlineData(404, ErrorType.NotFound)]
        [InlineData(410, ErrorType.NotFound)]
        public void ToErrorType_4xx_MapsToNotFound(int code, ErrorType expected)
        {
            Assert.Equal(expected, StatusCodeMapping.ToErrorType((HttpStatusCode)code));
        }

        [Fact]
        public void ToErrorType_408_ReturnsTimeout()
        {
            Assert.Equal(ErrorType.Timeout, StatusCodeMapping.ToErrorType(HttpStatusCode.RequestTimeout));
        }

        [Fact]
        public void ToErrorType_409_ReturnsConflict()
        {
            Assert.Equal(ErrorType.Conflict, StatusCodeMapping.ToErrorType(HttpStatusCode.Conflict));
        }

        [Fact]
        public void ToErrorType_422_ReturnsUnprocessableEntity()
        {
            Assert.Equal(ErrorType.UnprocessableEntity, StatusCodeMapping.ToErrorType((HttpStatusCode)422));
        }

        [Fact]
        public void ToErrorType_423_ReturnsLocked()
        {
            Assert.Equal(ErrorType.Locked, StatusCodeMapping.ToErrorType((HttpStatusCode)423));
        }

        [Fact]
        public void ToErrorType_424_ReturnsFailedDependency()
        {
            Assert.Equal(ErrorType.FailedDependency, StatusCodeMapping.ToErrorType((HttpStatusCode)424));
        }

        [Fact]
        public void ToErrorType_426_ReturnsUpgradeRequired()
        {
            Assert.Equal(ErrorType.UpgradeRequired, StatusCodeMapping.ToErrorType((HttpStatusCode)426));
        }

        [Fact]
        public void ToErrorType_428_ReturnsPreconditionRequired()
        {
            Assert.Equal(ErrorType.PreconditionRequired, StatusCodeMapping.ToErrorType((HttpStatusCode)428));
        }

        [Fact]
        public void ToErrorType_429_ReturnsTooManyRequests()
        {
            Assert.Equal(ErrorType.TooManyRequests, StatusCodeMapping.ToErrorType((HttpStatusCode)429));
        }

        [Fact]
        public void ToErrorType_451_ReturnsUnavailableForLegal()
        {
            Assert.Equal(ErrorType.UnavailableForLegal, StatusCodeMapping.ToErrorType((HttpStatusCode)451));
        }
    }

    #endregion

    #region 4xx Custom / Unmapped Codes

    public class FourXxCustomCodes
    {
        [Fact]
        public void ToErrorType_418_ImATeapot_ReturnsUnknown()
        {
            Assert.Equal(ErrorType.Unknown, StatusCodeMapping.ToErrorType((HttpStatusCode)418));
        }

        [Theory]
        [InlineData(420)]
        [InlineData(430)]
        [InlineData(440)]
        [InlineData(444)]
        [InlineData(450)]
        [InlineData(499)]
        public void ToErrorType_4xx_Unmapped_ReturnsValidation(int code)
        {
            // Unmapped 4xx codes should fall back to Validation (best approximation for client errors)
            Assert.Equal(ErrorType.Validation, StatusCodeMapping.ToErrorType((HttpStatusCode)code));
        }
    }

    #endregion

    #region 5xx Server Errors — Specific Mappings

    public class FiveXxSpecificMappings
    {
        [Theory]
        [InlineData(500, ErrorType.ServerError)]
        [InlineData(501, ErrorType.ServerError)]
        [InlineData(505, ErrorType.ServerError)]
        [InlineData(506, ErrorType.ServerError)]
        [InlineData(507, ErrorType.ServerError)]
        [InlineData(508, ErrorType.ServerError)]
        [InlineData(510, ErrorType.ServerError)]
        [InlineData(511, ErrorType.ServerError)]
        public void ToErrorType_5xx_MapsToServerError(int code, ErrorType expected)
        {
            Assert.Equal(expected, StatusCodeMapping.ToErrorType((HttpStatusCode)code));
        }

        [Fact]
        public void ToErrorType_502_ReturnsBadGateway()
        {
            Assert.Equal(ErrorType.BadGateway, StatusCodeMapping.ToErrorType(HttpStatusCode.BadGateway));
        }

        [Fact]
        public void ToErrorType_503_ReturnsServiceUnavailable()
        {
            Assert.Equal(ErrorType.ServiceUnavailable, StatusCodeMapping.ToErrorType(HttpStatusCode.ServiceUnavailable));
        }

        [Fact]
        public void ToErrorType_504_ReturnsGatewayTimeout()
        {
            Assert.Equal(ErrorType.GatewayTimeout, StatusCodeMapping.ToErrorType(HttpStatusCode.GatewayTimeout));
        }
    }

    #endregion

    #region 5xx Custom / Unmapped Codes

    public class FiveXxCustomCodes
    {
        [Theory]
        [InlineData(509)]
        [InlineData(520)]
        [InlineData(521)]
        [InlineData(522)]
        [InlineData(598)]
        [InlineData(599)]
        public void ToErrorType_5xx_Unmapped_ReturnsServerError(int code)
        {
            // All unmapped 5xx codes should fall back to ServerError
            Assert.Equal(ErrorType.ServerError, StatusCodeMapping.ToErrorType((HttpStatusCode)code));
        }
    }

    #endregion

    #region Out-of-Range Codes

    public class OutOfRangeCodes
    {
        [Theory]
        [InlineData(100)]
        [InlineData(101)]
        [InlineData(200)]
        [InlineData(201)]
        [InlineData(204)]
        [InlineData(301)]
        [InlineData(302)]
        [InlineData(304)]
        public void ToErrorType_NonErrorCodes_ReturnsUnknown(int code)
        {
            // 1xx, 2xx, 3xx are not error codes → Unknown
            Assert.Equal(ErrorType.Unknown, StatusCodeMapping.ToErrorType((HttpStatusCode)code));
        }

        [Theory]
        [InlineData(600)]
        [InlineData(999)]
        public void ToErrorType_InvalidRange_ReturnsUnknown(int code)
        {
            // Codes outside 400-599 range → Unknown
            Assert.Equal(ErrorType.Unknown, StatusCodeMapping.ToErrorType((HttpStatusCode)code));
        }
    }

    #endregion

    #region Edge Cases

    public class EdgeCases
    {
        [Fact]
        public void ToErrorType_Zero_ReturnsUnknown()
        {
            Assert.Equal(ErrorType.Unknown, StatusCodeMapping.ToErrorType((HttpStatusCode)0));
        }

        [Fact]
        public void ToErrorType_Negative_ReturnsUnknown()
        {
            // Cast negative to HttpStatusCode (wraps around)
            // Just verify it doesn't throw and returns Unknown or fallback
            var result = StatusCodeMapping.ToErrorType((HttpStatusCode)(-1));
            // Negative cast wraps to large positive, which is outside 400-599 → Unknown
            Assert.Equal(ErrorType.Unknown, result);
        }
    }

    #endregion
}
