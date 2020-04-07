using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Responses.Tests
{
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

        public static Task<HttpResponseMessage> FakeRequest(int status, object content) =>
            Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
            {
                Content = new StringContent(JsonConvert.SerializeObject(content))
            });
    }
}