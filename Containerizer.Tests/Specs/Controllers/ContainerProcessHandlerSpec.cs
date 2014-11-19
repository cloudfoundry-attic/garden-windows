using System.Net.Http;
using System.Reflection;
using System.Web.WebSockets;
using Containerizer.Controllers;
using Containerizer.Facades;
using Containerizer.Services.Interfaces;
using Containerizer.Tests.Specs.Facades;
using Moq;
using NSpec;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace Containerizer.Tests
{
    internal class ContainerProcessHandlerSpec : nspec
    {
        private ContainerProcessHandler handler;
        private Mock<IProcessFacade> mockProcess;
        private ProcessStartInfo startInfo;

        private void before_each()
        {
            mockProcess = new Mock<IProcessFacade>();
            startInfo = new ProcessStartInfo();
            handler = new ContainerProcessHandler(mockProcess.Object);

            mockProcess.Setup(x => x.StartInfo).Returns(startInfo);
            mockProcess.Setup(x => x.Start());

            var bytes = Encoding.UTF8.GetBytes("some text");
            var stream = new StreamReader(new MemoryStream(bytes));
            mockProcess.Setup(x => x.StandardOutput).Returns(stream);
        }

        private void SendProcessOutputEvent(string message)
        {
            mockProcess.Raise(mock => mock.OutputDataReceived += null, Helpers.CreateMockDataReceivedEventArgs(message));
        }

        private string WaitForWebSocketMessage(FakeWebSocket websocket)
        {
            while (websocket.LastSentBuffer.Array == null)
            {
                System.Threading.Thread.Yield();
            }
            var byteArray = websocket.LastSentBuffer.Array;
            return System.Text.Encoding.Default.GetString(byteArray);
        }

        private void describe_onmessage()
        {
            FakeWebSocket websocket = null;

            before = () =>
            {
                handler.WebSocketContext = new FakeAspNetWebSocketContext();
                websocket = (FakeWebSocket) handler.WebSocketContext.WebSocket;
                handler.OnMessage("{\"Path\":\"foo.exe\", \"Args\":[\"some\", \"args\"]}");
            };

            it["sets start info correctly"] = () =>
            {
                startInfo.FileName.should_be("foo.exe");
                startInfo.Arguments.should_be("some args");
            };

            it["runs something"] = () =>
            {
                mockProcess.Verify(x => x.Start());
            };

            context["when an event with data is triggered"] = () =>
            {
                it["sends over socket"] = () =>
                {
                    SendProcessOutputEvent("Hi");

                    var message = WaitForWebSocketMessage(websocket);
                    message.should_be("Hi");
                };
            };

            context["when an event without data is triggered (followed by data)"] = () =>
            {
                it["sends over socket"] = () =>
                {
                    SendProcessOutputEvent(null);
                    SendProcessOutputEvent("Second Event Data");

                    var message = WaitForWebSocketMessage(websocket);
                    message.should_be("Second Event Data");
                };
            };
        }
    }
}
