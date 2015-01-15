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
    internal class ConsumerCanDestoryContainerSpec : nspec
    {
        // Containerizer.Controllers.ContainersController containersController;
        private string id;
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
            Helpers.RemoveExistingSite(id, id);
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

                    handle = Guid.NewGuid() + "-" + Guid.NewGuid();
                    client.PostAsync("/api/Containers",
                        new StringContent("{\"Handle\": \"" + handle + "\"}")).Wait();
                };

                context["when I send a destroy request"] = () =>
                {
                   
                    HttpResponseMessage response = null;
                    before = () =>
                    {
                        response = client.DeleteAsync("/api/Containers/" + handle).GetAwaiter().GetResult();
                    };

                    it["responds with 200"] = () =>
                    {
                        response.StatusCode.should_be(HttpStatusCode.OK);
                    };

                    context["for a destroyed container"] = () =>
                    {
                        before = () =>
                        {
                            response = client.DeleteAsync("/api/Containers/" + handle).GetAwaiter().GetResult();

                        };

                        it["responds with 404"] = () =>
                        {
                            response.StatusCode.should_be(HttpStatusCode.NotFound);
                        };
                    };
                };
            };
        }
    }
}