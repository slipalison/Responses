using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Testing;

namespace Responses.Tests;

public static class TestUtils
{
    public static JsonElement ErrorJson(string code, string message, string layer, string applicationName)
    {
        var json = JsonSerializer.Serialize(new { code, message, layer, applicationName });
        return JsonDocument.Parse(json).RootElement;
    }

    public static Task<HttpResponseMessage> FakeRequest(int status, object? content, bool errorSerializable = true) =>
        Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = errorSerializable && content != null
                ? new StringContent(JsonSerializer.Serialize(content))
                : new StringContent("")
        });

    public static HttpTest SetupHttpTest(int status, object? content, bool errorSerializable = true)
    {
        var test = new HttpTest();
        if (content != null && errorSerializable)
            test.RespondWithJson(content, status);
        else
            test.RespondWith("", status);
        return test;
    }

    public static HttpTest SetupHttpTestForError(int status, string code, string message)
    {
        var test = new HttpTest();
        var obj = new { code, message };
        test.RespondWithJson(obj, status);
        return test;
    }
}
