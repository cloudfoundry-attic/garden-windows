using Containerizer.Facades;
using Containerizer.Models;
using Microsoft.Web.WebSockets;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Containerizer.Controllers
{
    public class ContainerProcessHandler : WebSocketHandler
    {
        private readonly IProcessFacade process; 

        public ContainerProcessHandler(IProcessFacade process)
        {
            this.process = process;
        }

        public override void OnMessage(string message)
        {
            var streamEvent = JsonConvert.DeserializeObject<ProcessStreamEvent>(message);
            var processSpec = streamEvent.ApiProcessSpec;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.FileName = processSpec.Path;
            process.StartInfo.Arguments = processSpec.Arguments();

            process.OutputDataReceived += OutputDataHandler;
            process.ErrorDataReceived += OutputErrorDataHandler;

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        private void OutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data == null) return;
            string data = JsonConvert.SerializeObject(new ProcessStreamEvent
            {
                MessageType = "stdout",
                Message = outLine.Data
            }, Formatting.None);
            this.Send(data);
        }

        private void OutputErrorDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data == null) return;
            string data = JsonConvert.SerializeObject(new ProcessStreamEvent
            {
                MessageType = "stderr",
                Message = outLine.Data
            }, Formatting.None);
            this.Send(data);
        }
    }
}