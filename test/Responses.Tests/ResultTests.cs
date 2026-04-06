using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Flurl.Http.Testing;
using Responses.Http;
using Xunit;

namespace Responses.Tests;

public class ResultTests
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
        var result = await TestUtils.FakeRequest(status, null)
            .ReceiveResult();
        Assert.True(result.IsSuccess);

        // Flurl HttpTest version
        using var test = new HttpTest();
        test.RespondWith("", status);
        var flurlResult = await "http://test".GetAsync().ReceiveResult();
        Assert.True(flurlResult.IsSuccess);
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
        // HttpResponseMessage version
        var result = await TestUtils.FakeRequest(status, null)
            .ReceiveResult();
        Assert.False(result.IsSuccess);
        Assert.Equal(status.ToString(), result.Error.Code);

        // Flurl HttpTest version
        using var test = new HttpTest();
        test.RespondWith("", status);
        var flurlResult = await "http://test".GetAsync().ReceiveResult();
        Assert.False(flurlResult.IsSuccess);
        Assert.Equal(status.ToString(), flurlResult.Error.Code);
    }
}
