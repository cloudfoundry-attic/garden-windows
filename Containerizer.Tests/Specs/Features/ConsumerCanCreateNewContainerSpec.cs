using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Web.Administration;
using Newtonsoft.Json.Linq;
using NSpec;

namespace Containerizer.Tests.Specs.Features
{
    internal class ConsumerCanCreateNewContainerSpec : nspec
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

        private void describe_consumer_can_create_new_container()
        {
            HttpClient client = null;
            ServerManager serverManager = null;

            context["given that I am a consumer of the api"] = () =>
            {
                before = () =>
                {
                    client = new HttpClient {BaseAddress = new Uri("http://localhost:" + port)};
                };

                context["when I post a request"] = () =>
                {
                    string handle = null;
                    before = () =>
                    {
                        handle = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString();
                        Task<HttpResponseMessage> postTask = client.PostAsync("/api/Containers",
                            new StringContent("{Handle: \"" + handle + "\"}"));
                        postTask.Wait();
                        HttpResponseMessage postResult = postTask.Result;
                        Task<string> readTask = postResult.Content.ReadAsStringAsync();
                        readTask.Wait();
                        string response = readTask.Result;
                        JObject json = JObject.Parse(response);
                        id = json["id"].ToString();
                    };

                    it["should receive the container's id in the response"] = () => { id.should_not_be_empty(); };

                    it["the response id should equal the passed in handle"] = () =>
                    {
                        id.should_be(handle);
                    };

                    describe["observable IIS side effects"] = () =>
                    {
                        before = () => { serverManager = ServerManager.OpenRemote("localhost"); };

                        it["should see a new site with the container's id"] =
                            () => { serverManager.Sites.should_contain(x => x.Name == id); };

                        it["the site should have a new app pool with same name as the container's id"] =
                            () =>
                            {
                                serverManager.Sites.First(x => x.Name == id).Applications[0].ApplicationPoolName
                                    .should_be(id.Replace("-", "").Substring(0,64));
                            };
                    };
                };
            };
        }
    }
}