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
    internal class ConsumerCanAddAPortOnAContainerSpec : nspec
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
                    client = new HttpClient { BaseAddress = new Uri("http://localhost:" + port) };
                };

                after = () =>
                {
                    Helpers.RemoveExistingSite("Containerizer.Tests", "ContainerizerTestsApplicationPool");
                };

                context["And there exists a container with a given id"] = () =>
                {
                    string containerId = null;
                    int containerPort = 5656;
                    before = () => { containerId = Helpers.CreateContainer(client); };
                    after = () => { Helpers.RemoveExistingSite(containerId, containerId); };

                    context["And there is no service listening on a given port"] = () =>
                    {
                        before = () =>
                        {
                            Helpers.PortIsUsed(containerPort).should_be_false();
                        };

                        context["When I POST a request to /api/containers/:id/net/in with a given port in the body"] = () =>
                        {
                            JObject json = null;
                            HttpResponseMessage postResult = null;

                            before = () =>
                            {
                                postResult = client.PostAsync("/api/containers/" + containerId + "/net/in",
                                new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                            {
                                new KeyValuePair<string, string>("hostPort", containerPort.ToString())
                                
                            })).GetAwaiter().GetResult();
                                json = JObject.Parse(postResult.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                            };

                            it["Then I receive expected response body"] = () =>
                            {
                                json["error"].should_be_null();
                                json["hostPort"].Value<int>().should_be(containerPort);
                            };

                            it["And I receive a successful code as a response"] = () =>
                            {
                                postResult.IsSuccessStatusCode.should_be_true();
                            };

                            context["When I start the server process (FIXME ; should I be needed?)"] = () =>
                            {
                                before = () =>
                                {
                                    var serverManager = ServerManager.OpenRemote("localhost");
                                    var existingSite = serverManager.Sites.First(x => x.Name == containerId);
                                    existingSite.Start();
                                    serverManager.CommitChanges();
                                };
                                
                                it["Then I can connect to the server listening on the given port"] = () =>
                                {
                                    Helpers.PortIsUsed(containerPort).should_be_true();
                                };
                            };
                        };
                    };
                };
            };
        }
    }
}
