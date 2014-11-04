using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;
using System.IO;

namespace Containerizer.Tests
{
    public static class Helpers
    {
        public static void SetupSiteInIIS(string applicationFolderName, string siteName, string applicationPoolName, int port)
        {
            ServerManager serverManager = new ServerManager();
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, applicationFolderName);

            Helpers.RemoveExistingSite(siteName, applicationPoolName);

            Site mySite = serverManager.Sites.Add(siteName, path, port);
            mySite.ServerAutoStart = true;

            serverManager.ApplicationPools.Add(applicationPoolName);
            mySite.Applications[0].ApplicationPoolName = applicationPoolName;
            ApplicationPool apppool = serverManager.ApplicationPools[applicationPoolName];
            apppool.ManagedRuntimeVersion = "v4.0";
            apppool.ManagedPipelineMode = ManagedPipelineMode.Integrated;

            serverManager.CommitChanges();
        }

        public static void RemoveExistingSite(string siteName, string applicationPoolName)
        {
            try
            {
                ServerManager serverManager = new ServerManager();
                var existingSite = serverManager.Sites.FirstOrDefault(x => x.Name == siteName);
                if (existingSite != null)
                {
                    serverManager.Sites.Remove(existingSite);
                    serverManager.CommitChanges();
                }

                var existingAppPool = serverManager.ApplicationPools.FirstOrDefault(x => x.Name == applicationPoolName);
                if (existingAppPool != null)
                {
                    serverManager.ApplicationPools.Remove(existingAppPool);
                    serverManager.CommitChanges();
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new Exception("Try running Visual Studio/test runner as Administrator instead.", ex);
            }
        }
    }
}
