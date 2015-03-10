using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;

namespace ServiceManager
{
    [RunInstaller(true)]
    public partial class Containerizer : System.Configuration.Install.Installer
    {
        private const string serviceName = "containerizer";

        public Containerizer()
        {
            InitializeComponent();
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            var userName = "containerizer";
            var password = CreateSecurePassword();

            CreateNewAdminUser(userName, password);
            Console.Out.WriteLine("Created new user and set password to {0}", password);
            SetupService(userName, password);
        }

        private static string CreateSecurePassword()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[32];
                rng.GetBytes(tokenData);
                return Convert.ToBase64String(tokenData);
            }
        }

        /*
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);
            System.Diagnostics.Process.Start("http://www.microsoft.com");
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
        }
        */

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);

            var workingDir = CodeBaseDirectory();
            var commands = new string[][] {
                new string[]{Path.Combine(workingDir, "nssm.exe"), string.Format("stop {0}", serviceName)},
                new string[]{Path.Combine(workingDir, "nssm.exe"), string.Format("remove {0} confirm", serviceName)},
            };
            RunCommands(workingDir, commands);
            var ctx = new PrincipalContext(ContextType.Machine);
            var user = UserPrincipal.FindByIdentity(ctx, "containerizer");
            user.Delete();
        }


        private void SetupService(string userName, string password)
        {
            var workingDir = CodeBaseDirectory();
            var containerizerExe = Path.Combine(workingDir, "bin", "Containerizer.exe");
            var commands = new string[][] {
                new string[]{Path.Combine(workingDir, "nssm.exe"), string.Format("install {0} \"{1}\" 80", serviceName, containerizerExe)},
                new string[]{Path.Combine(workingDir, "nssm.exe"), string.Format("set {0} Description \"Containerizer is the windows end of windows-garden for CF .Net\"", serviceName)},
                new string[]{GetFullPath("sc.exe"), string.Format("config {0} obj= \".\\{1}\" password= {2}", serviceName, userName, password)},
                new string[]{Path.Combine(workingDir, "nssm.exe"), string.Format("set {0} AppStdout \"{1}\"", serviceName, Path.Combine(workingDir, "containerizer.stdout.log"))},
                new string[]{Path.Combine(workingDir, "nssm.exe"), string.Format("set {0} AppStderr \"{1}\"", serviceName, Path.Combine(workingDir, "containerizer.stderr.log"))},
                new string[]{Path.Combine(workingDir, "nssm.exe"), string.Format("start {0}", serviceName)},
            };
            RunCommands(workingDir, commands);
        }

        private static string CodeBaseDirectory()
        {
            return Path.GetFullPath(Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", ""), ".."));
        }

        private static void RunCommands(string workingDir, string[][] commands)
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
                if (process.ExitCode != 0)
                {
                    throw new Exception(process.StandardOutput.ReadToEnd());
                }

            }
        }

        private void CreateNewAdminUser(string userName, string password)
        {
            var builtinAdminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            var ctx = new PrincipalContext(ContextType.Machine);
            var user = UserPrincipal.FindByIdentity(ctx, userName);
            if (user != null)
            {
                user.Delete();
            }
            user = new UserPrincipal(ctx, userName, password, true);
            var group = GroupPrincipal.FindByIdentity(ctx, builtinAdminSid.Value);
            group.Members.Add(user);
            user.Save();
            group.Save();
            GrantUserLogOnAsAService(userName);
        }

        private static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(';'))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }

        private void GrantUserLogOnAsAService(string userName)
        {
            try
            {
                LsaWrapper lsaUtility = new LsaWrapper();

                lsaUtility.SetRight(userName, "SeServiceLogonRight");

                Console.WriteLine("Logon as a Service right is granted successfully to " + userName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
