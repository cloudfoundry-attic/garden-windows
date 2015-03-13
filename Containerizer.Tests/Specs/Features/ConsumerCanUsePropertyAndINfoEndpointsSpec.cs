#region

using System;
using System.Collections.Generic;
using System.Net.Http;
using NSpec;
using Newtonsoft.Json.Linq;

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
                Helpers.ContainarizerProcess process = null;

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
            };
        }

        private static void DeletesProperty(HttpClient client, string handle, Dictionary<string,string> properties)
        {
            var infoPath = "/api/containers/" + handle + "/info";
            client.DeleteAsync("/api/containers/" + handle + "/properties/hello").Wait();

            var infoResponse = client.GetAsync(infoPath).Result.Content.ReadAsJson()["Properties"] as JObject;
            infoResponse.Count.should_be(1);

            var showResponse = client.GetAsync("/api/containers/" + handle + "/properties/mysecret").Result.Content.ReadAsJson();
            showResponse["value"].ToString().should_be(properties["mysecret"]);
        }

        private static void GetsEachProperty(HttpClient client, string handle, Dictionary<string, string> properties)
        {
            foreach (var p in properties)
            {
                var result = client.GetAsync("/api/containers/" + handle + "/properties/" + p.Key).Result.Content.ReadAsJson();
                result["value"].ToString().should_be(p.Value);
            }
        }

        private static void GetsInfo(HttpClient client, Helpers.ContainarizerProcess process, string handle, Dictionary<string, string> properties)
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