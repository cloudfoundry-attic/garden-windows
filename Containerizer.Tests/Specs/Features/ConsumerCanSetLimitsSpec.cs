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
                        var result = client.PostAsJsonAsync("/api/containers/" + handle + "/limit_memory", new { limit_in_bytes = 456456456 }).Result;

                        result.IsSuccessStatusCode.should_be_true();
                    };
                };
            };
        }
    }
}