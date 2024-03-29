﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http;

namespace Responses.Tests;

public class TestUtils
{
    public static JObject ErrorJson(string code, string message, string layer, string applicationName)
    {
        return JObject.FromObject(new
        {
            code,
            message,
            layer,
            applicationName
        });
    }

    public static Task<HttpResponseMessage> FakeRequest(int status, object content, bool errorSerializeble = true) =>
        Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = errorSerializeble ? new StringContent(JsonConvert.SerializeObject(content)) : new StringContent("")
        });

    public static async Task<IFlurlResponse> FakeRequestFlurl(int status, object content, bool errorSerializeble = true) =>
        await Task.FromResult(new FlurlResponse(await FakeRequest(status,content)));
}
