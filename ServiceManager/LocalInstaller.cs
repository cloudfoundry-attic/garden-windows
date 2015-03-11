using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace ServiceManager
{
    [RunInstaller(false)]
    public partial class LocalInstaller : System.Configuration.Install.Installer
    {
        private string exeArguments;
        private string executable;

        protected string serviceName;

        public LocalInstaller(string serviceName, string exeArguments)
            : this(serviceName, serviceName + ".exe", exeArguments)
        {
        }

        public LocalInstaller(string serviceName, string executable, string exeArguments)
        {
            this.serviceName = serviceName;
            this.executable = executable;
            this.exeArguments = exeArguments;
            InitializeComponent();
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            var workingDir = CodeBaseDirectory();
            var fileName = Path.Combine(workingDir, executable);
            var commands = new string[][] {
                new string[]{Path.Combine(workingDir, "nssm.exe"), string.Format("install {0} \"{1}\" {2}", serviceName, fileName, exeArguments)},              
                new string[]{Path.Combine(workingDir, "nssm.exe"), string.Format("set {0} Description \"{0} for CF .Net\"", serviceName)},
                new string[]{Path.Combine(workingDir, "nssm.exe"), string.Format("set {0} AppStdout \"{1}\"", serviceName, Path.Combine(workingDir, serviceName + ".stdout.log"))},
                new string[]{Path.Combine(workingDir, "nssm.exe"), string.Format("set {0} AppStderr \"{1}\"", serviceName, Path.Combine(workingDir, serviceName + ".stderr.log"))},
            };
            RunCommands(workingDir, commands);

            PreServiceStart();

            commands = new string[][] {
                new string[]{Path.Combine(workingDir, "nssm.exe"), string.Format("start {0}", serviceName)},
            };
            RunCommands(workingDir, commands);
        }
        public virtual void PreServiceStart() { }

        protected static string CodeBaseDirectory()
        {
            return Path.GetFullPath(Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", ""), ".."));
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);

            var workingDir = CodeBaseDirectory();
            var commands = new string[][] {
                new string[]{Path.Combine(workingDir, "nssm.exe"), string.Format("stop {0}", serviceName)},
                new string[]{Path.Combine(workingDir, "nssm.exe"), string.Format("remove {0} confirm", serviceName)},
            };
            RunCommands(workingDir, commands, false);
        }

        protected static void RunCommands(string workingDir, string[][] commands, bool throwOnFailure = true)
        {
            foreach (var cmd in commands)
            {
                Console.WriteLine("Executing {0} {1}", cmd[0], cmd[1]);

                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = cmd[0],
                        Arguments = cmd[1],
                        WorkingDirectory = workingDir,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                    }
                };

                process.Start();
                process.WaitForExit();
                if (throwOnFailure && process.ExitCode != 0)
                {
                    throw new Exception(process.StandardOutput.ReadToEnd());
                }

            }
        }
    }
}
