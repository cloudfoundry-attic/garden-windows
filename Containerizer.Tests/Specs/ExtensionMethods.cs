#region

using System.Net.Http;
using System.Threading;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs
{
    public static class ExtensionMethods
    {
        public static HttpResponseMessage Synchronously(this IHttpActionResult sender)
        {
            return sender.ExecuteAsync(new CancellationToken()).GetAwaiter().GetResult();
        }

        public static void VerifiesSuccessfulStatusCode(this HttpResponseMessage sender)
        {
            sender.IsSuccessStatusCode.should_be_true();
        }

        public static void VerifiesSuccessfulStatusCode(this IHttpActionResult sender)
        {
            sender.ExecuteAsync(new CancellationToken()).Result.IsSuccessStatusCode.should_be_true();
        }

        public static string ReadAsString(this HttpContent content)
        {
            return content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        public static JObject ReadContentAsJson(this IHttpActionResult sender)
        {
            return sender.ExecuteAsync(new CancellationToken()).Result.Content.ReadAsJson();
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