using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

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

        protected override void OnBeforeInstall(IDictionary savedState)
        {
            base.OnBeforeInstall(savedState);

            var missing = new List<string>();

            var required = new List<string>() {
                "CONTAINERIZER_USERNAME",
                "CONTAINERIZER_PASSWORD",
                "EXTERNAL_IP",
                "CONSUL_IPS",
                "ETCD_CLUSTER",
                "MACHINE_NAME",
                "ZONE",
                "STACK"
            };

            foreach (var key in required) {
                if (Context.Parameters[key] == null || Context.Parameters[key] == "")
                    missing.Add(key);
            }

            if(missing.Count > 0) {
                throw new Exception("Please provide all of the following msiexec properties: " + string.Join(", ", missing));
            }

            writePropertiesToFile(required);
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

        private void writePropertiesToFile(List<string> keys)
        {
            var parameters = new Dictionary<string, string>();
            foreach (string key in keys.Where(x => x != "CONTAINERIZER_PASSWORD"))
            {
                parameters.Add(key, Context.Parameters[key]);
            }
            var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            string jsonString = javaScriptSerializer.Serialize(parameters);
            var configFile = System.IO.Path.GetFullPath(System.IO.Path.Combine(Context.Parameters["assemblypath"], "..", "parameters.json"));
            System.IO.File.WriteAllText(configFile, jsonString);
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

