using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FeatureManager
{
    [RunInstaller(true)]
    public partial class FeatureManager : System.Configuration.Install.Installer
    {
        private const string eventSource = "Diego MSI Windows Features Installer";

        public FeatureManager()
        {
            InitializeComponent();

            if (!EventLog.SourceExists(eventSource))
                EventLog.CreateEventSource(eventSource, "Application");
            EventLog.WriteEntry(eventSource, "Service Initializing", EventLogEntryType.Information, 0);
        }


        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            var dismPath = Environment.ExpandEnvironmentVariables(@"%WINDIR%\SysNative\dism.exe");

            // enable windows features that the containerizer depends on in order to run the container host
            var commands = new string[][] {
		        new string[]{dismPath, "/online /Enable-Feature /FeatureName:IIS-WebServer /All /NoRestart"},
				new string[]{dismPath, "/online /Enable-Feature /FeatureName:IIS-WebSockets /All /NoRestart"},
				new string[]{dismPath, "/online /Enable-Feature /FeatureName:Application-Server-WebServer-Support /FeatureName:AS-NET-Framework /All /NoRestart"},
				new string[]{dismPath, "/online /Enable-Feature /FeatureName:IIS-HostableWebCore /All /NoRestart"},
		    };

            RunCommands(commands);
        }

        private void RunCommands(string[][] commands)
        {
            foreach (var cmd in commands)
            {
                EventLog.WriteEntry(eventSource, "Enable feature " + cmd[1], EventLogEntryType.Information, 0);

                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = cmd[0],
                        Arguments = cmd[1],
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                    }
                };

                process.Start();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    var message = "ERROR Enabling feature " + cmd[1] + "\r\n\r\n";
                    message += process.StandardOutput.ReadToEnd() + "\r\n\r\n";
                    message += process.StandardError.ReadToEnd();
                    EventLog.WriteEntry(eventSource, message, EventLogEntryType.Error, 0);
                }
            }
        }
    }
}

