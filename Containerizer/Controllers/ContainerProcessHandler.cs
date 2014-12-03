using System;
using System.Diagnostics;
using System.Linq.Expressions;
using Containerizer.Facades;
using Containerizer.Models;
using Containerizer.Services.Interfaces;
using Microsoft.Web.WebSockets;
using Newtonsoft.Json;

namespace Containerizer.Controllers
{
    public class ContainerProcessHandler : WebSocketHandler
    {
        private readonly IProcessFacade process;
        private readonly string containerRoot;

        public ContainerProcessHandler(string containerId, IContainerPathService pathService, IProcessFacade process)
        {
            containerRoot = pathService.GetContainerRoot(containerId);
            this.process = process;
        }

        public override void OnMessage(string message)
        {
            var streamEvent = JsonConvert.DeserializeObject<ProcessStreamEvent>(message);

            if (streamEvent.MessageType == "run" && streamEvent.ApiProcessSpec != null)
            {
                ApiProcessSpec processSpec = streamEvent.ApiProcessSpec;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.WorkingDirectory = containerRoot;
                process.StartInfo.FileName = containerRoot + '\\' + processSpec.Path;
                process.StartInfo.Arguments = processSpec.Arguments();
                process.OutputDataReceived += OutputDataHandler;
                process.ErrorDataReceived += OutputErrorDataHandler;

                try
                {
                    process.Start();
                }
                catch (Exception e)
                {
                    SendEvent("error", e.Message);
                    return;
                }
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.EnableRaisingEvents = true;
                process.Exited += ProcessExitedHandler;
            }
            else if (streamEvent.MessageType == "stdin")
            {
                process.StandardInput.Write(streamEvent.Data);
            }
        }

        private void SendEvent(string messageType, string message)
        {
            string data = JsonConvert.SerializeObject(new ProcessStreamEvent
            {
                MessageType = messageType,
                Data = message
            }, Formatting.None);
            Send(data);
        }

        private void OutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data == null) return;
            SendEvent("stdout", outLine.Data + "\r\n");
        }

        private void OutputErrorDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data == null) return;
            SendEvent("stderr", outLine.Data + "\r\n");
        }

        private void ProcessExitedHandler(object sendingProcess, System.EventArgs e)
        {
            SendEvent("close", null);
        }
    }
}