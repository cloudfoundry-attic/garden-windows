#region

using System.Net.Http;
using System.Threading;
using System.Web.Http;
using Newtonsoft.Json.Linq;

#endregion

namespace Containerizer.Tests.Specs
{
    public static class ExtensionMethods
    {
        public static HttpResponseMessage Synchronously(this IHttpActionResult sender)
        {
            return sender.ExecuteAsync(new CancellationToken()).GetAwaiter().GetResult();
        }

        public static string ReadAsString(this HttpContent content)
        {
            return content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        public static JObject ReadAsJson(this HttpContent content)
        {
            return JObject.Parse(content.ReadAsString());
        }

        public static JArray ReadAsJsonArray(this HttpContent content)
        {
            return JArray.Parse(content.ReadAsString());
        }
    }
}