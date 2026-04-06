using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Testing;

namespace Responses.Tests;

public static class TestUtils
{
    public static Task<HttpResponseMessage> FakeRequest(int status, object? content, bool errorSerializable = true) =>
        Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = errorSerializable && content != null
                ? new StringContent(JsonSerializer.Serialize(content))
                : new StringContent("")
        });
}
