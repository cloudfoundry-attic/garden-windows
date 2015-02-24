using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Web.Administration;
using Newtonsoft.Json.Linq;
using NSpec;

namespace Containerizer.Tests.Specs.Features
{
    internal class ConsumerCanDestroyContainerSpec : nspec
    {
        private void describe_consumer_can_destroy_a_container()
        {
            Helpers.ContainarizerProcess process = null;
            HttpClient client = null;
            string handle = null;

            before = () => process = Helpers.CreateContainerizerProcess();
            after = () => process.Dispose();

            context["given that I am a consumer of the api and a container exists"] = () =>
            {
                before = () =>
                {
                    client = process.GetClient();

                    handle = Helpers.CreateContainer(client);
                };

                context["when I send two destroy request"] = () =>
                {
                    it["responds with 200 and then 404"] = () =>
                    {
                        var response = client.DeleteAsync("/api/containers/" + handle).GetAwaiter().GetResult();
                        response.StatusCode.should_be(HttpStatusCode.OK);

                        response = client.DeleteAsync("/api/containers/" + handle).GetAwaiter().GetResult();
                        response.StatusCode.should_be(HttpStatusCode.NotFound);
                    };
                };
            };
        }
    }
}