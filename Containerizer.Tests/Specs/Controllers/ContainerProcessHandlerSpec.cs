#region

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Containerizer.Controllers;
using Containerizer.Tests.Specs.Facades;
using IronFoundry.Container;
using Moq;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    /*
    class ContainerProcessHandlerSpec : nspec
    {
        private readonly int expectedHostPort = 6336;
        private bool allowProcessToFinish = false;
        private string containerId;
        private Mock<IContainerDirectory> mockContainerDirectory;
        private Mock<IContainer> mockContainer;
        private Mock<IContainerService> mockContainerService;
        private ContainerProcessHandler handler;
        FakeWebSocket websocket = null;

        private void before_each()
        {
            containerId = new Guid().ToString();

            mockContainerDirectory = new Mock<IContainerDirectory>();
            mockContainerDirectory.Setup(x => x.MapUserPath("")).Returns(@"C:\A\Directory\user");

            mockContainer = new Mock<IContainer>();
            mockContainer.Setup(x => x.Directory).Returns(mockContainerDirectory.Object);

            mockContainerService = new Mock<IContainerService>();
            mockContainerService.Setup(x => x.GetContainerByHandle(containerId)).Returns(mockContainer.Object);

            handler = new ContainerProcessHandler(containerId, mockContainerService.Object);
            handler.WebSocketContext = new FakeAspNetWebSocketContext();
            websocket = (FakeWebSocket)handler.WebSocketContext.WebSocket;
        }

        private void describe_onmessage()
        {
            Mock<IContainerProcess> mockContainerProcess = null;
            ProcessSpec processSpec = null;
            IProcessIO processIO = null;

            context["container run succeeds"] = () =>
            {
                before = () =>
                {
                    allowProcessToFinish = false;
                    mockContainerProcess = new Mock<IContainerProcess>();
                    mockContainerProcess.Setup(x => x.WaitForExit()).Callback(() =>
                    {
                        while (!allowProcessToFinish)
                            Task.Delay(10);
                    }).Returns(null);
                    processSpec = null;
                    processIO = null;
                    mockContainer.Setup(x => x.Run(It.IsAny<ProcessSpec>(), It.IsAny<IProcessIO>()))
                        .Callback<ProcessSpec, IProcessIO>((pspec, pio) =>
                        {
                            processSpec = pspec;
                            processIO = pio;
                        })
                        .Returns(mockContainerProcess.Object);
                };

                act = () =>
                {
                    processSpec = null;
                    var messageBytes =
                        new UTF8Encoding(true).GetBytes(
                            "{\"type\":\"run\", \"pspec\":{\"Path\":\"foo.exe\", \"Args\":[\"some\", \"args\"]}}");
                    var message = new ArraySegment<Byte>(messageBytes);
                    handler.OnMessageReceived(message, WebSocketMessageType.Text);
                    while (processSpec == null) Thread.Sleep(10);
                };

                context["There is a single reserved port"] = () =>
                {
                    before = () =>
                    {
                        mockContainer.Setup(x => x.GetInfo()).Returns(
                            new ContainerInfo
                            {
                                ReservedPorts = new List<int> { expectedHostPort }
                            });
                    };

                    it["sets PORT on the environment variable"] = () =>
                    {
                        processSpec.Environment.ContainsKey("PORT").should_be_true();
                        processSpec.Environment["PORT"].should_be("6336");
                    };
                };

                context["when a port has not been reserved"] = () =>
                {
                    before = () =>
                    {
                        mockContainer.Setup(x => x.GetInfo()).Returns(
                            new ContainerInfo
                            {
                                ReservedPorts = new List<int>()
                            });
                    };

                    it["does not set PORT env variable"] = () =>
                    {
                        processSpec.Environment.ContainsKey("PORT").should_be_false();
                    };
                };

                it["sets working directory"] = () => { processSpec.WorkingDirectory.should_be("C:\\A\\Directory\\user"); };

                it["sets start info correctly"] = () =>
                {
                    processSpec.ExecutablePath.should_be("foo.exe");
                    processSpec.Arguments.should_be(new List<string> { "some", "args" });
                };

                it["stores command line arguments in ENV[ARGJSON]"] = () =>
                {
                    processSpec.Environment.ContainsKey("ARGJSON").should_be_true();
                    processSpec.Environment["ARGJSON"].should_be("[\"some\",\"args\"]");
                };

                it["runs something"] =
                    () => { mockContainer.Verify(x => x.Run(It.IsAny<ProcessSpec>(), It.IsAny<IProcessIO>())); };

                xdescribe["standard in"] = () => { };

                describe["standard out"] = () =>
                {
                    it["sends over socket"] = () =>
                    {
                        processIO.StandardOutput.Write("Hi");

                        var message = WaitForWebSocketMessage(websocket);
                        message.should_be("{\"type\":\"stdout\",\"data\":\"Hi\\r\\n\"}");
                    };
                };

                describe["standard error"] = () =>
                {
                    it["sends over socket"] = () =>
                    {
                        processIO.StandardError.Write("Hi");

                        var message = WaitForWebSocketMessage(websocket);
                        message.should_be("{\"type\":\"stderr\",\"data\":\"Hi\\r\\n\"}");
                    };
                };

                describe["once the process exits"] = () =>
                {
                    it["sends close event over socket"] = () =>
                    {
                        allowProcessToFinish = true;

                        var message = WaitForWebSocketMessage(websocket);
                        message.should_be("{\"type\":\"close\"}");
                    };
                };
            };

            context["when process.start raises an error"] = () =>
            {
                before = () =>
                    mockContainer.Setup(mock => mock.Run(It.IsAny<ProcessSpec>(), It.IsAny<IProcessIO>()))
                        .Throws(new Exception("An Error Message"));

                act = () =>
                    handler.OnMessage("{\"type\":\"run\", \"pspec\":{\"Path\":\"foo.exe\", \"Args\":[\"some\", \"args\"]}}");

                it["sends the error over the socket"] = () =>
                {
                    var message = WaitForWebSocketMessage(websocket);
                    message.should_be("{\"type\":\"error\",\"data\":\"An Error Message\"}");
                };
            };
        }

        private void after_each()
        {
            allowProcessToFinish = true;
        }

        private string WaitForWebSocketMessage(FakeWebSocket websocket)
        {
            for (var i = 0; i < 20; i++)
            {
                Thread.Sleep(10);
                if (websocket.LastSentBuffer.Array != null)
                {
                    var byteArray = websocket.LastSentBuffer.Array;
                    return Encoding.Default.GetString(byteArray);
                }
            }
            return "no message sent (test)";
        }
    }
     */
}
