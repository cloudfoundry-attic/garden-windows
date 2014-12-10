using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Containerizer.Services.Implementations;
using NSpec;

namespace Containerizer.Tests.Specs.Features
{
    internal class PingSpec : nspec
    {
        private int port;
        private HttpClient client;
        private string containerPath;
        private string id;

        private void before_each()
        {
            port = 8088;
            Helpers.SetupSiteInIIS("Containerizer", "Containerizer.Tests", "ContainerizerTestsApplicationPool", port,
                true);
            client = new HttpClient { BaseAddress = new Uri("http://localhost:" + port) };
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