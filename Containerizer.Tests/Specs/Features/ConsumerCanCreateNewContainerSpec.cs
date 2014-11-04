using System;
using System.Collections.Generic;
using NSpec;
using System.Linq;
using System.Web.Http.Results;
using System.IO;
using Microsoft.Web.Administration;
using System.Net.Http;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;

namespace Containerizer.Tests
{
    class ConsumerCanCreateNewContainerSpec : nspec
    {
        Containerizer.Controllers.ContainersController containersController;
        int port;
        string id;

        void before_each()
        {
            port = 8088;
            Helpers.SetupSiteInIIS("Containerizer", "Containerizer.Tests", "ContainerizerTestsApplicationPool", port);
        }

        void after_each()
        {
            Helpers.RemoveExistingSite("Containerizer.Tests", "ContainerizerTestsApplicationPool");
            Helpers.RemoveExistingSite(id, id);
        }

        void describe_consumer_can_create_new_container()
        {
            HttpClient client = null;
            string response = null;
            ServerManager serverManager = null;

            context["given that I am a consumer of the api"] = () =>
            {
                before = () =>
                {
                    client = new HttpClient();
                    client.BaseAddress = new Uri("http://localhost:" + port.ToString());
                };

                context["when I post a request"] = () =>
                {
                    before = () =>
                    {
                        var postTask = client.PostAsync("/api/Containers", new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()));
                        postTask.Wait();
                        var postResult = postTask.Result;
                        var readTask = postResult.Content.ReadAsStringAsync();
                        readTask.Wait();
                        response = readTask.Result;
                        var json = JObject.Parse(response);
                        id = json["id"].ToString();
                    };

                    it["should receive the container's id in the response"] = () =>
                    {
                        id.should_not_be_empty();
                    };

                    describe["observable IIS side effects"] = () =>
                    {
                        before = () =>
                        {
                            serverManager = new ServerManager();
                        };

                        it["should see a new site with the container's id"] = () =>
                        {
                            serverManager.Sites.should_contain(x => x.Name == id);
                        };

                        it["the site should have a new app pool with same name as the container's id"] = () =>
                        {
                            serverManager.Sites.First(x => x.Name == id).Applications[0].ApplicationPoolName.should_be(id);
                        };
                    };
                };
            };
        }
    }
}


