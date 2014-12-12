using System;
using System.Net.Http;
using NSpec;

namespace Containerizer.Tests.Specs.Features
{
    internal class PingSpec : nspec
    {
        private HttpClient client;
        private string containerPath;
        private string id;
        private int port;

        private void before_each()
        {
            port = 8088;
            Helpers.SetupSiteInIIS("Containerizer", "Containerizer.Tests", "ContainerizerTestsApplicationPool", port,
                true);
            client = new HttpClient {BaseAddress = new Uri("http://localhost:" + port)};
        }

        private void after_each()
        {
            Helpers.RemoveExistingSite("Containerizer.Tests", "ContainerizerTestsApplicationPool");
        }

        private void describe_ping()
        {
            context["given that containerizer is running"] = () =>
            {
                describe["ping"] = () =>
                {
                    it["succeeds"] = () =>
                    {
                        HttpResponseMessage getTask = client.GetAsync("/api/Ping").GetAwaiter().GetResult();
                        getTask.IsSuccessStatusCode.should_be_true();
                    };
                };
            };
        }
    }
}