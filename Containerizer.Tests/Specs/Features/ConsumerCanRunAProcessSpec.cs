#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs.Features
{
    internal class ConsumerCanRunAProcessSpec : nspec
    {
        private void describe_consumer_can_run_a_process()
        {
            ClientWebSocket client = null;
            HttpClient httpClient = null;
            string handle = null;
            Helpers.ContainarizerProcess process = null;

            before = () =>
            {
                process = Helpers.CreateContainerizerProcess();
                httpClient = process.GetClient();
                handle = Helpers.CreateContainer(httpClient);
            };
            after = () =>
            {
                Helpers.DestroyContainer(httpClient, handle);
                process.Dispose();
            };

            context["given that I am a consumer of the api"] = () =>
            {
                string containerPath = null;
                var hostPort = 0;

                context["I have a file to run in my container"] = () =>
                {
                    before = () =>
                    {
                        containerPath = Helpers.GetContainerPath(handle);
                        File.WriteAllBytes(containerPath + "/myfile.bat",
                            new UTF8Encoding(true).GetBytes(
                                "@echo off\r\n@echo Hi Fred\r\n@echo Jane is good\r\n@echo Jill is better\r\nset PORT\r\n"));

                        var response =
                            httpClient.PostAsJsonAsync("/api/containers/" + handle + "/net/in", new {hostPort = 0})
                                .GetAwaiter()
                                .GetResult();
                        var json = response.Content.ReadAsJson();
                        hostPort = json["hostPort"].Value<int>();

                        client = new ClientWebSocket();
                        try
                        {
                            client.ConnectAsync(
                                new Uri("ws://localhost:" + process.Port + "/api/containers/" + handle + "/run"),
                                CancellationToken.None).GetAwaiter().GetResult();
                        }
                        catch (WebSocketException ex)
                        {
                            throw new Exception("Make sure to enable websockets following instructions in the README.",
                                ex);
                        }
                    };

                    describe["when I send a start request"] = () =>
                    {
                        List<String> messages = null;

                        before = () =>
                        {
                            var encoder = new UTF8Encoding();
                            var buffer =
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
                                    var message = Encoding.Default.GetString(receiveBuffer);
                                    if (message.Contains("error"))
                                    {
                                        throw new Exception("websocket returned an error message");
                                    }
                                    messages.Add(message.Substring(0, result.Count));
                                }
                            } while (messages.Count < 7);
                        };

                        it["should run a process, return stdout and close the socket"] = () =>
                        {
                            messages.should_contain("{\"type\":\"stdout\",\"data\":\"Hi Fred\\r\\n\"}");

                            messages.should_contain("{\"type\":\"stdout\",\"data\":\"PORT=" + hostPort + "\\r\\n\"}");

                            messages.should_contain("{\"type\":\"close\",\"data\":\"0\"}");
                        };
                    };
                };
            };
        }
    }
}