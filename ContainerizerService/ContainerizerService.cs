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

namespace ContainerizerService
{
    public partial class ContainerizerService : ServiceBase
    {
        private Process process;
        private const string eventSource = "Containerizer";

        public ContainerizerService()
        {
            InitializeComponent();

            if (!EventLog.SourceExists(eventSource))
                EventLog.CreateEventSource(eventSource, "Application");
            EventLog.WriteEntry(eventSource, "Service Initializing", EventLogEntryType.Information, 0);
        }

        protected override void OnStart(string[] args)
        {
            process = new Process
            {
                StartInfo =
                {
                    FileName =  @"bin\Containerizer.exe",
                    Arguments = @"10.10.5.81 1788",
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
    }
}
