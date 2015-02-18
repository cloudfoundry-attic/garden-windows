#region

using System;
using System.Collections.Generic;
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
    internal class ContainerProcessHandlerSpec : nspec
    {
        private readonly int expectedHostPort = 6336;
        private bool allowProcessToFinish;
        private string containerId;
        private byte[] fakeStandardInput;
        private ContainerProcessHandler handler;
        private Mock<IContainer> mockContainer;
        private Mock<IContainerDirectory> mockContainerDirectory;
        private Mock<IContainerProcess> mockContainerProcess;
        private Mock<IContainerService> mockContainerService;
        private IProcessIO processIO;
        private ProcessSpec processSpec;

        private void before_each()
        {
            mockContainerService = new Mock<IContainerService>();
            mockContainerDirectory = new Mock<IContainerDirectory>();

            containerId = new Guid().ToString();

            mockContainer = new Mock<IContainer>();
            mockContainerService.Setup(x => x.GetContainerByHandle(containerId)).Returns(mockContainer.Object);
            mockContainer.Setup(x => x.GetInfo()).Returns(
                new ContainerInfo
                {
                    ReservedPorts = new List<int> {expectedHostPort}
                });


            mockContainer.Setup(x => x.Directory).Returns(mockContainerDirectory.Object);
            mockContainerDirectory.Setup(x => x.MapUserPath("")).Returns(@"C:\A\Directory\user");

            allowProcessToFinish = false;
            mockContainerProcess = new Mock<IContainerProcess>();
            mockContainerProcess.Setup(x => x.WaitForExit()).Callback(() =>
            {
                while (!allowProcessToFinish)
                    Task.Delay(10);
            }).Returns(null);
            mockContainer.Setup(x => x.Run(It.IsAny<ProcessSpec>(), It.IsAny<IProcessIO>()))
                .Callback<ProcessSpec, IProcessIO>((processSpec, processIO) =>
                {
                    this.processSpec = processSpec;
                    this.processIO = processIO;
                })
                .Returns(mockContainerProcess.Object);

            handler = new ContainerProcessHandler(containerId, mockContainerService.Object);
        }

        private void after_each()
        {
            allowProcessToFinish = true;
        }

        private void SendProcessOutputEvent(string message)
        {
            processIO.StandardOutput.Write(message);
        }

        private void SendProcessErrorEvent(string message)
        {
            processIO.StandardError.Write(message);
        }

        private void SendProcessExitEvent()
        {
            allowProcessToFinish = true;
        }

        private string WaitForWebSocketMessage(FakeWebSocket websocket)
        {
            Thread.Sleep(100);
            if (websocket.LastSentBuffer.Array == null)
            {
                return "no message sent (test)";
            }
            var byteArray = websocket.LastSentBuffer.Array;
            return Encoding.Default.GetString(byteArray);
        }

        private void describe_onmessage()
        {
            FakeWebSocket websocket = null;

            before = () =>
            {
                handler.WebSocketContext = new FakeAspNetWebSocketContext();
                websocket = (FakeWebSocket) handler.WebSocketContext.WebSocket;
            };

            act =
                () =>
                {
                    handler.OnMessage(
                        "{\"type\":\"run\", \"pspec\":{\"Path\":\"foo.exe\", \"Args\":[\"some\", \"args\"]}}");
                };

            it["sets working directory"] = () => { processSpec.WorkingDirectory.should_be("C:\\A\\Directory\\user"); };


            it["sets start info correctly"] = () =>
            {
                processSpec.ExecutablePath.should_be("foo.exe");
                processSpec.Arguments.should_be(new List<string> {"some", "args"});
            };

            it["sets PORT on the environment variable"] = () =>
            {
                processSpec.Environment.ContainsKey("PORT").should_be_true();
                processSpec.Environment["PORT"].should_be("6336");
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

                it["does not set PORT env variable"] =
                    () => { processSpec.Environment.ContainsKey("PORT").should_be_false(); };
            };

            it["runs something"] =
                () => { mockContainer.Verify(x => x.Run(It.IsAny<ProcessSpec>(), It.IsAny<IProcessIO>())); };


            context["when process.start raises an error"] = () =>
            {
                before = () =>
                    mockContainer.Setup(mock => mock.Run(It.IsAny<ProcessSpec>(), It.IsAny<IProcessIO>()))
                        .Throws(new Exception("An Error Message"));

                it["sends the error over the socket"] = () =>
                {
                    var message = WaitForWebSocketMessage(websocket);
                    message.should_be("{\"type\":\"error\",\"data\":\"An Error Message\"}");
                };
            };

            xdescribe["standard in"] = () => { };

            describe["standard out"] = () =>
            {
                it["sends over socket"] = () =>
                {
                    SendProcessOutputEvent("Hi");

                    var message = WaitForWebSocketMessage(websocket);
                    message.should_be("{\"type\":\"stdout\",\"data\":\"Hi\\r\\n\"}");
                };
            };

            describe["standard error"] = () =>
            {
                it["sends over socket"] = () =>
                {
                    SendProcessErrorEvent("Hi");

                    var message = WaitForWebSocketMessage(websocket);
                    message.should_be("{\"type\":\"stderr\",\"data\":\"Hi\\r\\n\"}");
                };
            };

            describe["once the process exits"] = () =>
            {
                it["sends close event over socket"] = () =>
                {
                    SendProcessExitEvent();

                    var message = WaitForWebSocketMessage(websocket);
                    message.should_be("{\"type\":\"close\"}");
                };
            };
        }
    }
}