#region

using System;
using System.Collections.Generic;
using System.Net.Http;
using NSpec;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using Containerizer.Controllers;

#endregion

namespace Containerizer.Tests.Specs.Features
{
    internal class ConsumerCanSetLimitsSpec : nspec
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

                    it["allows the consumer to get and set limits"] = () =>
                    {
                        var limit_in_bytes = 32UL * 1024 * 1024;
                        var url = "/api/containers/" + handle + "/memory_limit";
                        var result = client.PostAsJsonAsync(url, new { limit_in_bytes = limit_in_bytes }).Result;
                        result.IsSuccessStatusCode.should_be_true();
                        result = client.GetAsync(url).Result;
                        result.IsSuccessStatusCode.should_be_true();
                        var limits = JsonConvert.DeserializeObject<MemoryLimits>(result.Content.ReadAsString());
                        limits.LimitInBytes.should_be(limit_in_bytes);
                    };
                };
            };
        }
    }
}