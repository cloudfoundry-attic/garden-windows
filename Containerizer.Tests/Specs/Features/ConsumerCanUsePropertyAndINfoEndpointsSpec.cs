#region

using System;
using System.Collections.Generic;
using System.Net.Http;
using NSpec;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;

#endregion

namespace Containerizer.Tests.Specs.Features
{
    internal class ConsumerCanUsePropertyAndInfoEndpointsSpec : nspec
    {
        private void describe_()
        {
            context["Given that I'm a consumer of the containerizer API"] = () =>
            {
                HttpClient client = null;
                Helpers.ContainerizerProcess process = null;

                before = () =>
                {
                    process = Helpers.CreateContainerizerProcess();
                    client = process.GetClient();
                };

                after = () => process.Dispose();

                context["And there exists a container with a given id"] = () =>
                {
                    string handle = null;

                    before = () => handle = Helpers.CreateContainer(client);
                    after = () => Helpers.DestroyContainer(client, handle);

                    it["allows the consumer to interact with the property endpoints correctly"] = () =>
                    {
                        var properties = new Dictionary<string, string>
                        {
                            {"mysecret", "dontread"},
                            {"hello", "goodbye"},
                        };

                        PutsProperties(client, handle, properties);
                        GetsAllProperties(client, handle, properties);
                        GetsInfo(client, process, handle, properties);
                        GetsEachProperty(client, handle, properties);
                        DeletesProperty(client, handle, properties);
                    };
                };

                context["And there exists multiple containers"] = () =>
                {
                    string handle1 = null, handle2 = null;

                    before = () => { handle1 = Helpers.CreateContainer(client); handle2 = Helpers.CreateContainer(client); };
                    after = () => { Helpers.DestroyContainer(client, handle1); Helpers.DestroyContainer(client, handle2); };

                    it["bulkinfo returns info for the given handles"] = () =>
                    {
                        var handles = JsonConvert.SerializeObject(new string[] { handle1, handle2 });
                        var result = client.PostAsync("/api/bulkcontainerinfo", new StringContent(handles, Encoding.UTF8, "application/json")).Result;
                        result.IsSuccessStatusCode.should_be_true();
                        var response = result.Content.ReadAsJson();

                        response.Count.should_be(2);
                        response[handle1]["Info"]["ExternalIP"].ToString().should_be(process.ExternalIP);
                        response[handle2]["Info"]["ExternalIP"].ToString().should_be(process.ExternalIP);
                    };
                };
            };
        }

        private static void DeletesProperty(HttpClient client, string handle, Dictionary<string,string> properties)
        {
            var infoPath = "/api/containers/" + handle + "/info";
            client.DeleteAsync("/api/containers/" + handle + "/properties/hello").Wait();

            var infoResponse = client.GetAsync(infoPath).Result.Content.ReadAsJson()["Properties"] as JObject;
            infoResponse.Count.should_be(1);

            var showResponse = client.GetAsync("/api/containers/" + handle + "/properties/mysecret").Result.Content.ReadAsJsonString();
            showResponse.should_be(properties["mysecret"]);
        }

        private static void GetsEachProperty(HttpClient client, string handle, Dictionary<string, string> properties)
        {
            foreach (var p in properties)
            {
                var result = client.GetAsync("/api/containers/" + handle + "/properties/" + p.Key).Result.Content.ReadAsJsonString();
                result.should_be(p.Value);
            }
        }

        private static void GetsInfo(HttpClient client, Helpers.ContainerizerProcess process, string handle, Dictionary<string, string> properties)
        {
            var infoPath = "/api/containers/" + handle + "/info";
            var infoResponse = client.GetAsync(infoPath).Result.Content.ReadAsJson() as JObject;

            infoResponse["ExternalIP"].ToString().should_be(process.ExternalIP);
            infoResponse["Properties"].ToObject<Dictionary<string, string>>().should_be(properties);
        }

        private static void GetsAllProperties(HttpClient client, string handle, Dictionary<string, string> properties)
        {

            var propertiesPath = "/api/containers/" + handle + "/properties";
            var propertiesResponse = client.GetAsync(propertiesPath).Result.Content.ReadAsJson();
            propertiesResponse.ToObject<Dictionary<string, string>>().should_be(properties);
        }

        private static void PutsProperties(HttpClient client, string handle, Dictionary<string, string> properties)
        {
            foreach (var p in properties)
            {
                var result = client.PutAsync("/api/containers/" + handle + "/properties/" + p.Key, new StringContent(p.Value)).Result;
                result.IsSuccessStatusCode.should_be_true();
            }
        }
    }
}