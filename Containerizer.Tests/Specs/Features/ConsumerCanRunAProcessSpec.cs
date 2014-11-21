using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using NSpec;

namespace Containerizer.Tests.Specs.Features
{
    internal class ConsumerCanRunAProcessSpec : nspec
    {
        private int port;

        private void before_each()
        {
            port = 8088;
            Helpers.SetupSiteInIIS("Containerizer", "Containerizer.Tests", "ContainerizerTestsApplicationPool", port,
                true);
        }

        private void after_each()
        {
            Helpers.RemoveExistingSite("Containerizer.Tests", "ContainerizerTestsApplicationPool");
        }

        private void describe_consumer_can_run_a_process()
        {
            ClientWebSocket client = null;

            context["given that I am a consumer of the api"] = () =>
            {
                before = () =>
                {
                    client = new ClientWebSocket();
                    client.ConnectAsync(new Uri("ws://localhost:" + port + "/api/containers/AN_ID/run"),
                        CancellationToken.None).GetAwaiter().GetResult();
                };

                describe["when I send a start request"] = () =>
                {
                    it["should upgrade to a websocket"] = () =>
                    {
                        var encoder = new UTF8Encoding();
                        byte[] buffer =
                            encoder.GetBytes("{\"type\":\"run\", \"pspec\":{\"Path\":\"echo\", Args:[\"hello\"]}}");
                        client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                            CancellationToken.None);
                    };

                    it["should run a process"] = () =>
                    {
                        var encoder = new UTF8Encoding();
                        byte[] buffer =
                            encoder.GetBytes(
                                "{\"type\":\"run\", \"pspec\":{\"Path\":\"ipconfig.exe\", Args:[\"/all\"]}}");
                        client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                            CancellationToken.None);

                        var receiveBuffer = new byte[1024];
                        var receiveBufferSegment = new ArraySegment<byte>(receiveBuffer);
                        WebSocketReceiveResult result;
                        do
                        {
                            result =
                                client.ReceiveAsync(receiveBufferSegment, CancellationToken.None)
                                    .GetAwaiter()
                                    .GetResult();
                        } while (result.Count == 0);
                        string message = Encoding.Default.GetString(receiveBuffer);
                        message.should_start_with("{\"type\":\"stdout\",\"data\":\"\"}");

                        do
                        {
                            result =
                                client.ReceiveAsync(receiveBufferSegment, CancellationToken.None)
                                    .GetAwaiter()
                                    .GetResult();
                        } while (result.Count == 0);
                        message = Encoding.Default.GetString(receiveBuffer);
                        message.should_start_with("{\"type\":\"stdout\",\"data\":\"Windows IP Configuration\"}");
                    };
                };
            };
        }
    }
}