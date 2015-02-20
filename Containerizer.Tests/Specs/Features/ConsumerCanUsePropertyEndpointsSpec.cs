#region

using System;
using System.Collections.Generic;
using System.Net.Http;
using NSpec;
using Newtonsoft.Json.Linq;

#endregion

namespace Containerizer.Tests.Specs.Features
{
    internal class ConsumerCanUsePropertyEndpointsSpec : nspec
    {
        private void describe_()
        {
            context["Given that I'm a consumer of the containerizer API"] = () =>
            {
                HttpClient client = null;

                before = () =>
                {
                    int port = 8088;
                    Helpers.SetupSiteInIIS("Containerizer", "Containerizer.Tests", "ContainerizerTestsApplicationPool",
                        port, true);
                    client = new HttpClient {BaseAddress = new Uri("http://localhost:" + port)};
                };

                after =
                    () =>
                    {
                        Helpers.RemoveExistingSite("Containerizer.Tests", "ContainerizerTestsApplicationPool");
                    };

                context["And there exists a container with a given id"] = () =>
                {
                    string containerId = null;

                    before = () =>
                    {
                        containerId = Helpers.CreateContainer(client);
                    };
                    after = () =>
                    {
                        Helpers.RemoveExistingSite(containerId, containerId);
                    };

                    it["allows the consumer to interact with the property endpoints correctly"] = () =>
                    {
                        var properties = new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>("mysecret", "dontread"),
                            new KeyValuePair<string, string>("hello", "goodbye"),
                        };
                        Func<int, string> path =
                            n => "/api/containers/" + containerId + "/properties/" + properties[n].Key;
                        Func<int, StringContent> content = n => new StringContent(properties[n].Value);

                        var result1 = client.PutAsync(path(0), content(0)).Result;
                        var result2 = client.PutAsync(path(1), content(1)).Result;
                        result1.IsSuccessStatusCode.should_be_true();
                        result2.IsSuccessStatusCode.should_be_true();

                        var indexPath = "/api/containers/" + containerId + "/info";
                        var indexResponse = client.GetAsync(indexPath).Result.Content.ReadAsJson()["Properties"] as JObject;

                        Action<int> verifyIndex =
                            n => indexResponse[properties[n].Key].ToString().should_be(properties[n].Value);

                        verifyIndex(0);
                        verifyIndex(1);

                        client.DeleteAsync(path(1)).Wait();

                        indexResponse = client.GetAsync(indexPath).Result.Content.ReadAsJson()["Properties"] as JObject;
                        indexResponse.Count.should_be(1);

                        var showResponse = client.GetAsync(path(0)).Result.Content.ReadAsJson(); 
                        showResponse["value"].ToString().should_be(properties[0].Value);
                    };
                };
            };
        }
    }
}