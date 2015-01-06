#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Containerizer.Services.Implementations;
using NSpec;

#endregion

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
                string containerId = Guid.NewGuid().ToString();
                string containerPath = null;

                before = () =>
                {
                    containerPath = new ContainerPathService().GetContainerRoot(containerId);
                    Directory.CreateDirectory(containerPath);
                    File.WriteAllBytes(containerPath + "/myfile.bat",
                        new UTF8Encoding(true).GetBytes(
                            "@echo off\r\n@echo Hi Fred\r\n@echo Jane is good\r\n@echo Jill is better\r\n"));

                    client = new ClientWebSocket();
                    client.ConnectAsync(new Uri("ws://localhost:" + port + "/api/containers/" + containerId + "/run"),
                        CancellationToken.None).GetAwaiter().GetResult();
                };

                after = () => Directory.Delete(containerPath, true);

                describe["when I send a start request"] = () =>
                {
                    List<String> messages = null;

                    before = () =>
                    {
                        var encoder = new UTF8Encoding();
                        byte[] buffer =
                            encoder.GetBytes(
                                "{\"type\":\"run\", \"pspec\":{\"Path\":\"myfile.bat\", Args:[\"/all\"]}}");
                        client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                            CancellationToken.None);

                        messages = new List<string>();
                        var receiveBuffer = new byte[1024];
                        var receiveBufferSegment = new ArraySegment<byte>(receiveBuffer);
                        WebSocketReceiveResult result;
                        do
                        {
                            result =
                                client.ReceiveAsync(receiveBufferSegment, CancellationToken.None)
                                    .GetAwaiter()
                                    .GetResult();
                            if (result.Count > 0)
                            {
                                string message = Encoding.Default.GetString(receiveBuffer);
                                messages.Add(message.Substring(0, result.Count));
                            }
                        } while (messages.Count < 4);
                    };

                    it["should run a process, return stdout and close the socket"] = () =>
                    {
                        messages.should_contain("{\"type\":\"stdout\",\"data\":\"Hi Fred\\r\\n\"}");
                        messages.should_contain("{\"type\":\"close\"}");
                    };
                };
            };
        }
    }
}