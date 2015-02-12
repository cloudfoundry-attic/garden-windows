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
        // Containerizer.Controllers.ContainersController containersController;
        private int port;

        private void before_each()
        {
            port = 8088;
            Helpers.SetupSiteInIIS("Containerizer", "Containerizer.Tests", "ContainerizerTestsApplicationPool", port,
                true);
        }

        private void after_each()
        {
            Helpers.RemoveExistingSite("Containerizer.Tests", "ContainerizerTestsApplicationPool");
        }

        private void describe_consumer_can_destroy_a_container()
        {
            HttpClient client = null;
            string handle = null;

            context["given that I am a consumer of the api and a container exists"] = () =>
            {
                before = () =>
                {
                    client = new HttpClient { BaseAddress = new Uri("http://localhost:" + port) };

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