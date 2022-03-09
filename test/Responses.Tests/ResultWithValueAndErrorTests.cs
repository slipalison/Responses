using System.Threading.Tasks;
using Flurl.Http;
using Xunit;
using static Responses.Tests.TestUtils;
using Responses.Http;

namespace Responses.Tests
{
    public class ResultWithValueAndErrorTests
    {
        [Theory]
        [InlineData(200)]
        [InlineData(201)]
        [InlineData(204)]
        [InlineData(250)]
        [InlineData(299)]
        public async Task Ok(int status)
        {
            var result = await FakeRequest(status, true)
                .ReceiveResult<bool, Error>();

            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(401)]
        [InlineData(409)]
        [InlineData(450)]
        [InlineData(499)]
        [InlineData(500)]
        [InlineData(501)]
        [InlineData(550)]
        [InlineData(599)]
        public async Task Fail(int status)
        {
            var result = await FakeRequest(status, ErrorJson(status.ToString(), "ANY ERROR", "Core", "ANY1"))
                .ReceiveResult<bool, Error>();

            Assert.False(result.IsSuccess);
            Assert.Equal("Core", result.Error.Layer);
            Assert.Equal(status.ToString(), result.Error.Code);
            Assert.Equal("ANY ERROR", result.Error.Message);
            Assert.Equal("ANY1", result.Error.ApplicationName);
        }
    }
}
