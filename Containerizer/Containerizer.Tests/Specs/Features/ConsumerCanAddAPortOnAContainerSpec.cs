#region

using System.Net.Http;
using Newtonsoft.Json.Linq;
using NSpec;


#endregion

namespace Containerizer.Tests.Specs.Features
{
    internal class ConsumerCanAddAPortOnAContainerSpec : nspec
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
                    string containerId = null;
                    before = () =>
                    {
                        containerId = Helpers.CreateContainer(client);
                    };

                    after = () =>
                    {
                        Helpers.DestroyContainer(client, containerId);
                    };

                    context["When I POST a request to /api/containers/:id/net/in with a host port of 0 in the body"] = () =>
                    {
                        int resultingHostPort = 0;

                        before = () =>
                        {
                            var response = client.PostAsJsonAsync("/api/containers/" + containerId + "/net/in", new { hostPort = 0, containerPort = 8080 }).GetAwaiter().GetResult();
                            var json = response.Content.ReadAsJson();
                            resultingHostPort = json["hostPort"].Value<int>();
                        };

                        it["returns an assigned host port"] = () =>
                        {
                            resultingHostPort.should_not_be(0);
                        };

                        it["returns the assigned host ports on an INFO call and a LIE for the container port (see comment)"] = () =>
                        {
                            var response = client.GetAsync("/api/containers/" + containerId + "/info").GetAwaiter().GetResult();
                            var json = response.Content.ReadAsJson();
                            var portMapping = json["MappedPorts"][0];
                            portMapping["HostPort"].Value<int>().should_be(resultingHostPort);
                            portMapping["ContainerPort"].Value<int>().should_be(8080);
                        };

                    };
                };
            };
        }
    }
}
