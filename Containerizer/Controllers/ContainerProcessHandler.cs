#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Containerizer.Models;
using IronFoundry.Container;
using Microsoft.Web.WebSockets;
using Newtonsoft.Json;
using Owin.WebSocket;

#endregion

namespace Containerizer.Controllers
{
    public interface IWebSocketEventSender
    {
        void SendEvent(string messageType, string message);
    }

    public class ContainerProcessHandler : WebSocketConnection, IWebSocketEventSender
    {
        private readonly IContainerService containerService;

        public ContainerProcessHandler(IContainerService containerService)
        {
            this.containerService = containerService;
        }

        public void SendEvent(string messageType, string message)
        {
            var jsonString = JsonConvert.SerializeObject(new ProcessStreamEvent
            {
                MessageType = messageType,
                Data = message
            }, Formatting.None);
            var data = new UTF8Encoding(true).GetBytes(jsonString);
            SendText(data, true);
        }

        public override Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
            var bytes = new UTF8Encoding(true).GetString(message.Array, 0, message.Count);
            var streamEvent = JsonConvert.DeserializeObject<ProcessStreamEvent>(bytes);

            if (streamEvent.MessageType == "run" && streamEvent.ApiProcessSpec != null)
            {
                var handle = Arguments["handle"];
                var containerRoot = containerService.GetContainerByHandle(handle).Directory.UserPath;
                var container = containerService.GetContainerByHandle(handle);

                var processSpec = new ProcessSpec
                {
                    DisablePathMapping = false,
                    Privileged = false,
                    WorkingDirectory = containerRoot,
                    ExecutablePath = streamEvent.ApiProcessSpec.Path,
                    Environment = new Dictionary<string, string>
                    {
                        { "ARGJSON", JsonConvert.SerializeObject(streamEvent.ApiProcessSpec.Args) }
                    },
                    Arguments = streamEvent.ApiProcessSpec.Args
                };
                var info = container.GetInfo();
                if (info != null && info.ReservedPorts.Count > 0)
                    processSpec.Environment["PORT"] = info.ReservedPorts[0].ToString();

                return Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var processIO = new ProcessIO(this);
                        var process = container.Run(processSpec, processIO);
                        Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                process.WaitForExit();
                            } catch (OperationCanceledException) { }
                            SendEvent("close", null);
                        });
                    }
                    catch (Exception e)
                    {
                        SendEvent("error", e.Message);
                    }
                });
            }
            return null;
        }

        private class WSWriter : TextWriter
        {
            private readonly string streamName;
            private readonly IWebSocketEventSender ws;

            public WSWriter(string streamName, IWebSocketEventSender ws)
            {
                this.streamName = streamName;
                this.ws = ws;
            }

            public override Encoding Encoding
            {
                get { return Encoding.Default; }
            }

            public override void Write(string value)
            {
                ws.SendEvent(streamName, value + "\r\n");
            }
        }

        private class ProcessIO : IProcessIO
        {
            public ProcessIO(IWebSocketEventSender ws)
            {
                StandardOutput = new WSWriter("stdout", ws);
                StandardError = new WSWriter("stderr", ws);
            }

            public TextWriter StandardOutput { get; set; }
            public TextWriter StandardError { get; set; }
            public TextReader StandardInput { get; set; }
        }
    }
}