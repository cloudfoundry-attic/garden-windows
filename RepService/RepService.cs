using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RepService
{
    public partial class RepService : ServiceBase
    {
        private Process process;
        private const string eventSource = "Rep";

        public RepService()
        {
            InitializeComponent();

            if (!EventLog.SourceExists(eventSource))
                EventLog.CreateEventSource(eventSource, "Application");
            EventLog.WriteEntry(eventSource, "Service Initializing", EventLogEntryType.Information, 0);
        }

        protected override void OnStart(string[] args)
        {
            var hash = parameters();

            process = new Process
            {
                StartInfo =
                {
                    FileName = "rep.exe",
                    Arguments = " -etcdCluster=" + hash["ETCD_CLUSTER"] + " -debugAddr=0.0.0.0:17008 -stack=" + hash["STACK"] + " -executorURL=http://127.0.0.1:1700 " +
                                " -listenAddr=0.0.0.0:1800 -cellID=" + hash["MACHINE_NAME"] + " -zone=" + hash["ZONE"] + " -pollingInterval=30s -evacuationTimeout=180s",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                }
            };

            process.EnableRaisingEvents = true;
            process.Exited += process_Exited;

            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => EventLog.WriteEntry(eventSource, e.Data, EventLogEntryType.Information, 0);
            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => EventLog.WriteEntry(eventSource, e.Data, EventLogEntryType.Warning, 0);

            EventLog.WriteEntry(eventSource, "Starting", EventLogEntryType.Information, 0);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        void process_Exited(object sender, EventArgs e)
        {
            EventLog.WriteEntry(eventSource, "Exiting", EventLogEntryType.Error, 0);
            this.ExitCode = 0XDEAD;
            this.Stop();
        }

        protected override void OnStop()
        {
            base.OnStop();
            if (!process.HasExited)
            {
                process.Kill();
            }
        }

        protected Dictionary<string, string> parameters()
        {
            var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var jsonString = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "parameters.json");
            return javaScriptSerializer.Deserialize<Dictionary<string, string>>(jsonString);
        }
    }
}
