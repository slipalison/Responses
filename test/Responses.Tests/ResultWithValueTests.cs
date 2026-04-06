using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Flurl.Http.Testing;
using Responses.Http;
using Xunit;

namespace Responses.Tests;

public class ResultWithValueTests
{
    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(204)]
    [InlineData(250)]
    [InlineData(299)]
    public async Task Ok(int status)
    {
        // HttpResponseMessage version
        var result = await TestUtils.FakeRequest(status, true)
            .ReceiveResult<bool>();
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        // Flurl HttpTest version
        using var test = new HttpTest();
        test.RespondWith("true", status);
        var flurlResult = await "http://test".GetAsync().ReceiveResult<bool>();
        Assert.True(flurlResult.IsSuccess);
        Assert.True(flurlResult.Value);
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
        var result = await TestUtils.FakeRequest(status, null)
            .ReceiveResult<bool>();

        Assert.False(result.IsSuccess);
        // Error code should match status code from HttpResponseMessage
        Assert.True(result.Error.Code == status.ToString() || result.Error.Code == "HttpError");
    }
}
