#region

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NSpec;
using System.Text;

#endregion

namespace Containerizer.Tests.Specs.Features
{




    internal class ConsumerCanCreateNewContainerSpec : nspec
    {
        // Containerizer.Controllers.ContainersController containersController;
        private string id;
        private Helpers.ContainerizerProcess process;

        private void before_each()
        {
            process = Helpers.CreateContainerizerProcess();
        }

        private void after_each()
        {
            Helpers.DestroyContainer(process.GetClient(), id);
            process.Dispose();
        }

        private void describe_consumer_can_create_new_container()
        {
            HttpClient client = null;

            context["given that I am a consumer of the api"] = () =>
            {
                before = () =>
                {
                    client = process.GetClient();
                };

                context["when I post a request without env vars"] = () =>
                {
                    string handle = null;
                    string propertyKey = "awesome";
                    string propertyValue = "sauce";

                    before = () =>
                    {
                        handle = Guid.NewGuid() + "-" + Guid.NewGuid();
                        Task<HttpResponseMessage> postTask = client.PostAsync("/api/containers",
                            new StringContent(
                                "{\"Handle\": \"" + handle + "\", \"Properties\":{\"" + propertyKey + "\":\"" +
                                propertyValue + "\"}}", Encoding.UTF8, "application/json"));
                        postTask.Wait();
                        HttpResponseMessage postResult = postTask.Result;
                        Task<string> readTask = postResult.Content.ReadAsStringAsync();
                        readTask.Wait();
                        string response = readTask.Result;
                        JObject json = JObject.Parse(response);
                        id = json["handle"].ToString();
                    };

                    it["creates the container"] = () =>
                    {
                        id.should_be(handle);

                        var listResponse = client.GetAsync("/api/containers").Result.Content.ReadAsJsonArray();
                        listResponse.Values<string>().Contains(handle).should_be_true();

                        var propertyResponse =
                            client.GetAsync("/api/containers/" + handle + "/properties/" + propertyKey)
                                .Result.Content.ReadAsJsonString();
                        propertyResponse.should_be(propertyValue);
                    };
                };
            };
        }
    }
}