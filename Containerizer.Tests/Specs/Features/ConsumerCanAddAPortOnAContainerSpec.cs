#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.Web.Administration;
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

                before = () =>
                {
                    // Create a container (POST)

                    int port = 8088;
                    Helpers.SetupSiteInIIS("Containerizer", "Containerizer.Tests", "ContainerizerTestsApplicationPool",
                        port, true);
                    client = new HttpClient { BaseAddress = new Uri("http://localhost:" + port) };
                };

                after =
                    () =>
                    {
                        // Nuke the container
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
                        Helpers.DestroyContainer(client, containerId);
                    };

                    context["When I POST a request to /api/containers/:id/net/in with a host port of 0 in the body"] =
                        () =>
                        {
                            int resultingHostPort = 0;

                            before = () =>
                            {
                                var response = client.PostAsJsonAsync("/api/containers/" + containerId + "/net/in", new { hostPort = 0 }).GetAwaiter().GetResult();
                                var json = response.Content.ReadAsJson();
                                resultingHostPort = json["hostPort"].Value<int>();
                            };

                            it["returns an assigned host port"] = () =>
                            {
                                resultingHostPort.should_not_be(0);
                            };

                            it["returns the assigned host ports on an INFO call and a LIE for the container port (see comment)"] = () =>
                            /* IMPORTANT NOTE!!!  The container port will always be reported as 8080.  THIS IS A LIE. 
                            * Since Windows can't namespace its network ports like Linux, the HostPort and the ContainerPort are the SAME. *gasp*
                            * We report 8080 back because NetIn from Diego always requests port 8080 as a ContainerPort and then expects INFO to tell us that 8080 is indeed the ContainerPort.  *facepalm*
                            * We can get away with this subterfuge because we control how apps are run in the container and give them the truth while lying to the outside world. *wink* */
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