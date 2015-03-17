using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ExecutorService
{
    public partial class ExecutorService : ServiceBase
    {
        private Process process;
        private const string eventSource = "Executor";

        public ExecutorService()
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
                    FileName = "executor.exe",
                    Arguments = " -listenAddr=0.0.0.0:1700 -skipCertVerify=false -debugAddr=0.0.0.0:17004 -gardenNetwork=tcp -gardenAddr=localhost:9241 " +
                                " -memoryMB=auto -diskMB=auto -containerMaxCpuShares=1024 -tempDir=/c/tmp/executor/tmp -cachePath=/c/tmp/executor/cache -maxCacheSizeInBytes=10000000000 " +
                                " -allowPrivileged=false -exportNetworkEnvVars=false -drainTimeout=900s -healthyMonitoringInterval=30s -unhealthyMonitoringInterval=0.5s",
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
