using Containerizer.Facades;
using Containerizer.Services.Implementations;
using Containerizer.Services.Interfaces;
using Microsoft.Web.WebSockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace Containerizer.Controllers
{
    public class ApiProcessSpec
    {
        public string Path { get; set; }
        public string[] Args { get; set; }
    }

    public class ContainerProcessHandler : WebSocketHandler
    {
        private readonly IProcessFacade process; 

        public ContainerProcessHandler(IProcessFacade process)
        {
            this.process = process;
        }

        public override void OnMessage(string message)
        {
            var ser = new JavaScriptSerializer();
            var processSpec = ser.Deserialize<ApiProcessSpec>(message);

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName = processSpec.Path;
            process.StartInfo.Arguments = String.Join(" ", processSpec.Args);
            process.Start();
        }
    }
}