using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using IronFrame;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Containerizer.Services.Implementations
{
    public class RunService : IRunService
    {
        public IContainer container { get; set; }

        public void Run(Controllers.IWebSocketEventSender websocket, Models.ApiProcessSpec apiProcessSpec)
        {
            container.StartGuard();

            var processSpec = NewProcessSpec(apiProcessSpec);
            var info = container.GetInfo();
            if (info != null)
            {
                CopyProcessSpecEnvVariables(processSpec, apiProcessSpec.Env);
                OverrideEnvPort(processSpec, info);
            }

            var process = Run(websocket, processSpec);
            if (process != null)
                WaitForExit(websocket, process);
        }

        private static void WaitForExit(IWebSocketEventSender websocket, IContainerProcess process)
        {
            try
            {
                var exitCode = process.WaitForExit();
                websocket.SendEvent("close", exitCode.ToString());
                websocket.Close(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "process finished");
            }
            catch (Exception e)
            {
                websocket.SendEvent("close", "-1");
                websocket.Close(System.Net.WebSockets.WebSocketCloseStatus.InternalServerError, e.Message);
            }
        }

        private IContainerProcess Run(IWebSocketEventSender websocket, ProcessSpec processSpec)
        {
            try
            {
                var processIO = new ProcessIO(websocket);
                var process = container.Run(processSpec, processIO);
                websocket.SendEvent("pid", process.Id.ToString());
                return process;
            }
            catch (Exception e)
            {
                websocket.SendEvent("error", e.Message);
                websocket.Close(System.Net.WebSockets.WebSocketCloseStatus.InternalServerError, e.Message);
                return null;
            }
        }

        private void OverrideEnvPort(ProcessSpec processSpec, ContainerInfo info)
        {
            if (processSpec.Environment.ContainsKey("PORT"))
            {
                var hostport = container.GetProperty("ContainerPort:" + processSpec.Environment["PORT"]);
                processSpec.Environment["PORT"] = hostport;
            }
            else if (processSpec.Arguments != null)
            {
                // TODO: Remove this. This case is for Healthcheck ; when nsync sends port as env variable we can remove this. (https://github.com/cloudfoundry-incubator/nsync/pull/1)
                var portArg = Array.Find(processSpec.Arguments, s => s.StartsWith("-port="));
                if (portArg != null)
                {
                    var containerPort = portArg.Split(new Char[] {'='})[1];
                    var hostport = container.GetProperty("ContainerPort:" + containerPort);
                    processSpec.Environment["PORT"] = hostport;
                }
            }
        }

        private ProcessSpec NewProcessSpec(Models.ApiProcessSpec apiProcessSpec)
        {
            var processSpec = new ProcessSpec
            {
                DisablePathMapping = false,
                Privileged = false,
                WorkingDirectory = container.Directory.UserPath,
                ExecutablePath = apiProcessSpec.Path,
                Environment = new Dictionary<string, string>
                    {
                        { "USERPROFILE", container.Directory.UserPath },
                        { "ARGJSON", JsonConvert.SerializeObject(apiProcessSpec.Args) }
                    },
                Arguments = apiProcessSpec.Args
            };
            return processSpec;
        }

        private static void CopyProcessSpecEnvVariables(ProcessSpec processSpec, string[] envStrings)
        {
            if (envStrings == null) { return; }
            foreach (var kv in envStrings)
            {
                string[] arr = kv.Split(new Char[] { '=' }, 2);
                processSpec.Environment[arr[0]] = arr[1];
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