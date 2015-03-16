using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using IronFoundry.Container;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Containerizer.Services.Implementations
{
    public class RunService : IRunService
    {
        public IContainer container { get; set; }

        public Task OnMessageReceived(Controllers.IWebSocketEventSender websocket, Models.ApiProcessSpec apiProcessSpec)
        {

            var processSpec = new ProcessSpec
            {
                DisablePathMapping = false,
                Privileged = false,
                WorkingDirectory = container.Directory.UserPath,
                ExecutablePath = apiProcessSpec.Path,
                Environment = new Dictionary<string, string>
                    {
                        { "ARGJSON", JsonConvert.SerializeObject(apiProcessSpec.Args) }
                    },
                Arguments = apiProcessSpec.Args
            };
            var info = container.GetInfo();
            if (info != null && info.ReservedPorts.Count > 0)
                processSpec.Environment["PORT"] = info.ReservedPorts[0].ToString();

            return Task.Factory.StartNew(() =>
            {
                try
                {
                    var processIO = new ProcessIO(websocket);
                    var process = container.Run(processSpec, processIO);
                    int exitCode = -1;
                    try
                    {
                        exitCode = process.WaitForExit();
                    }
                    catch (OperationCanceledException e)
                    {
                        // SendEvent("error", e.Message);
                    }
                    websocket.SendEvent("close", exitCode.ToString());
                }
                catch (Exception e)
                {
                    websocket.SendEvent("error", e.Message);
                }
            });
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