#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Containerizer.Models;
using IronFoundry.Container;
using Microsoft.Web.WebSockets;
using Newtonsoft.Json;

#endregion

namespace Containerizer.Controllers
{
    public interface IWebSocketEventSender
    {
        void SendEvent(string messageType, string message);
    }

    public class ContainerProcessHandler : WebSocketHandler, IWebSocketEventSender
    {
        private readonly IContainer container;
        private readonly string containerRoot;

        public ContainerProcessHandler(string containerId, IContainerService containerService)
        {
            containerRoot = containerService.GetContainerByHandle(containerId).Directory.MapUserPath("");
            container = containerService.GetContainerByHandle(containerId);
        }

        public void SendEvent(string messageType, string message)
        {
            var data = JsonConvert.SerializeObject(new ProcessStreamEvent
            {
                MessageType = messageType,
                Data = message
            }, Formatting.None);
            Send(data);
        }

        public override void OnMessage(string message)
        {
            var streamEvent = JsonConvert.DeserializeObject<ProcessStreamEvent>(message);

            if (streamEvent.MessageType == "run" && streamEvent.ApiProcessSpec != null)
            {
                var processSpec = new ProcessSpec
                {
                    DisablePathMapping = false,
                    Privileged = false,
                    WorkingDirectory = containerRoot,
                    ExecutablePath = streamEvent.ApiProcessSpec.Path,
                    Environment = new Dictionary<string, string>(),
                    Arguments = streamEvent.ApiProcessSpec.Args
                };
                var reservedPorts = container.GetInfo().ReservedPorts;
                if (reservedPorts.Count > 0)
                    processSpec.Environment["PORT"] = reservedPorts[0].ToString();

                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var process = container.Run(processSpec, new ProcessIO(this));
                        Task.Factory.StartNew(() =>
                        {
                            process.WaitForExit();
                            SendEvent("close", null);
                        });
                    }
                    catch (Exception e)
                    {
                        SendEvent("error", e.Message);
                    }
                }).GetAwaiter().GetResult();
            }
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