using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Containerizer.Controllers;
using Containerizer.Facades;
using Containerizer.Tests.Specs.Facades;
using Moq;
using NSpec;

namespace Containerizer.Tests.Specs.Controllers
{
    internal class ContainerProcessHandlerSpec : nspec
    {
        private byte[] fakeStandardInput;
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

            fakeStandardInput = new byte[4096];
            var stream = new StreamWriter(new MemoryStream(fakeStandardInput)) {AutoFlush = true};
            mockProcess.Setup(x => x.StandardInput).Returns(stream);
        }

        private void SendProcessOutputEvent(string message)
        {
            mockProcess.Raise(mock => mock.OutputDataReceived += null, Helpers.CreateMockDataReceivedEventArgs(message));
        }

        private void SendProcessErrorEvent(string message)
        {
            mockProcess.Raise(mock => mock.ErrorDataReceived += null, Helpers.CreateMockDataReceivedEventArgs(message));
        }

        private string WaitForWebSocketMessage(FakeWebSocket websocket)
        {
            var tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            const int timeOut = 100; // 0.1s

            Task task = Task.Factory.StartNew(() =>
            {
                while (websocket.LastSentBuffer.Array == null)
                {
                    Thread.Yield();
                }
            }, token);

            if (!task.Wait(timeOut, token))
                return "no message sent (test)";

            byte[] byteArray = websocket.LastSentBuffer.Array;
            return Encoding.Default.GetString(byteArray);
        }

        private void describe_onmessage()
        {
            FakeWebSocket websocket = null;

            before = () =>
            {
                handler.WebSocketContext = new FakeAspNetWebSocketContext();
                websocket = (FakeWebSocket) handler.WebSocketContext.WebSocket;
                handler.OnMessage("{\"type\":\"run\", \"pspec\":{\"Path\":\"foo.exe\", \"Args\":[\"some\", \"args\"]}}");
            };

            it["sets start info correctly"] = () =>
            {
                startInfo.FileName.should_be("foo.exe");
                startInfo.Arguments.should_be("some args");
            };

            it["runs something"] = () => { mockProcess.Verify(x => x.Start()); };

            describe["standard in"] = () =>
            {
                it["writes the data from the socket to the process' stdin"] = () =>
                {
                    handler.OnMessage("{\"type\":\"stdin\", \"data\":\"stdin data\"}");
                    string fakeStdinString = Encoding.Default.GetString(fakeStandardInput);
                    fakeStdinString.should_start_with("stdin data");
                };
            };

            describe["standard out"] = () =>
            {
                context["when an event with data is triggered"] = () =>
                {
                    it["sends over socket"] = () =>
                    {
                        SendProcessOutputEvent("Hi");

                        string message = WaitForWebSocketMessage(websocket);
                        message.should_be("{\"type\":\"stdout\",\"data\":\"Hi\"}");
                    };
                };

                context["when an event without data is triggered"] = () =>
                {
                    it["sends an empty string over the socket"] = () =>
                    {
                        SendProcessOutputEvent(null);

                        string message = WaitForWebSocketMessage(websocket);
                        message.should_be("no message sent (test)");
                    };
                };
            };

            describe["standard error"] = () =>
            {
                context["when an event with data is triggered"] = () =>
                {
                    it["sends over socket"] = () =>
                    {
                        SendProcessErrorEvent("Hi");

                        string message = WaitForWebSocketMessage(websocket);
                        message.should_be("{\"type\":\"stderr\",\"data\":\"Hi\"}");
                    };
                };

                context["when an event without data is triggered"] = () =>
                {
                    it["sends an empty string over the socket"] = () =>
                    {
                        SendProcessErrorEvent(null);

                        string message = WaitForWebSocketMessage(websocket);
                        message.should_be("no message sent (test)");
                    };
                };
            };
        }
    }
}