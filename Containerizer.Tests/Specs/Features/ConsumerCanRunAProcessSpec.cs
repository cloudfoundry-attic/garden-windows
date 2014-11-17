using System;
using System.Collections.Generic;
using NSpec;
using System.Linq;
using System.Web.Http.Results;
using System.IO;
using Microsoft.Web.Administration;
using System.Net.Http;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Text;

namespace Containerizer.Tests
{
    class ConsumerCanRunAProcessSpec : nspec
    {
        Containerizer.Controllers.ContainersController containersController;
        int port;

        void before_each()
        {
            port = 8088;
            Helpers.SetupSiteInIIS("Containerizer", "Containerizer.Tests", "ContainerizerTestsApplicationPool", port, true);
        }

        void after_each()
        {
            Helpers.RemoveExistingSite("Containerizer.Tests", "ContainerizerTestsApplicationPool");
        }

        void describe_consumer_can_run_a_process()
        {
            ClientWebSocket client = null;
            string response = null;
            ServerManager serverManager = null;

            context["given that I am a consumer of the api"] = () =>
            {
                before = () =>
                {
                    client = new ClientWebSocket();
                    client.ConnectAsync(new Uri("ws://localhost:" + port.ToString() + "/api/containers/AN_ID/run"), CancellationToken.None).GetAwaiter().GetResult();
                };

                describe["when I send a start request"] = () =>
                {
                    it["should run a process"] = () =>
                    {
                        var encoder = new UTF8Encoding();
                        byte[] buffer = encoder.GetBytes("{\"Path\":\"echo\", Args:[\"hello\"]}");
                        client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    };
                };
            };
        }
    }
}