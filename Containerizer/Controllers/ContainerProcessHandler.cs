using System.Diagnostics;
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

                process.Start();
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

        private void OutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data == null) return;
            string data = JsonConvert.SerializeObject(new ProcessStreamEvent
            {
                MessageType = "stdout",
                Data = outLine.Data + "\r\n"
            }, Formatting.None);
            Send(data);
        }

        private void OutputErrorDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data == null) return;
            string data = JsonConvert.SerializeObject(new ProcessStreamEvent
            {
                MessageType = "stderr",
                Data = outLine.Data + "\r\n"
            }, Formatting.None);
            Send(data);
        }

        private void ProcessExitedHandler(object sendingProcess, System.EventArgs e)
        {
            string data = JsonConvert.SerializeObject(new ProcessStreamEvent
            {
               MessageType = "close"
            }, Formatting.None);
            Send(data);

            // this.Close();
        }
    }
}