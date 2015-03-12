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
    [RunInstaller(false)]
    public partial class Containerizer : LocalInstaller
    {
        private string username;
        private string password;

        public Containerizer() : base("containerizer", "bin\\containerizer.exe", "10.10.5.4 80")
        {
            username = "containerizer";
            password = CreateSecurePassword();
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Install(IDictionary stateSaver)
        {
            CreateNewAdminUser();
            // Console.Out.WriteLine("Created new user and set password to {0}", password);

            base.Install(stateSaver);
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);

            var ctx = new PrincipalContext(ContextType.Machine);
            var user = UserPrincipal.FindByIdentity(ctx, serviceName);
            user.Delete();
        }

        public override void PreServiceStart()
        {
            var workingDir = CodeBaseDirectory();
            var commands = new string[][] {
                new string[]{GetFullPath("sc.exe"), string.Format("config {0} obj= \".\\{1}\" password= {2}", serviceName, username, password)},
            };
            RunCommands(workingDir, commands);
        }

        private string CreateSecurePassword()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[32];
                rng.GetBytes(tokenData);
                return Convert.ToBase64String(tokenData);
            }
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

        private void CreateNewAdminUser()
        {
            var builtinAdminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            var ctx = new PrincipalContext(ContextType.Machine);
            var user = UserPrincipal.FindByIdentity(ctx, username);
            if (user != null)
            {
                user.Delete();
            }
            user = new UserPrincipal(ctx, username, password, true);
            var group = GroupPrincipal.FindByIdentity(ctx, builtinAdminSid.Value);
            group.Members.Add(user);
            user.Save();
            group.Save();
            GrantUserLogOnAsAService();
        }

        private void GrantUserLogOnAsAService()
        {
            try
            {
                LsaWrapper lsaUtility = new LsaWrapper();

                lsaUtility.SetRight(username, "SeServiceLogonRight");

                Console.WriteLine("Logon as a Service right is granted successfully to " + username);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
