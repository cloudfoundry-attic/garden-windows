#region

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Containerizer.Services.Interfaces;
using Microsoft.Web.Administration;
using NSpec.Domain.Extensions;

#endregion

namespace Containerizer.Services.Implementations
{
    public class ContainerService : IContainerService
    {
        private IContainerPathService containerPathService;

        public ContainerService(IContainerPathService containerPathService)
        {
            this.containerPathService = containerPathService;
        }

        public string CreateContainer(String containerId)
        {
            try
            {
                ServerManager serverManager = ServerManager.OpenRemote("localhost");
                var path = containerPathService.GetContainerRoot(containerId);
                var appPoolName = HandleToAppPoolName(containerId);
                Site site = serverManager.Sites.Add(containerId, path, 0);

                serverManager.ApplicationPools.Add(appPoolName);
                site.Applications[0].ApplicationPoolName = appPoolName;
                ApplicationPool appPool = serverManager.ApplicationPools[appPoolName];
                appPool.ManagedPipelineMode = ManagedPipelineMode.Integrated;

                serverManager.CommitChanges();
                containerPathService.CreateContainerDirectory(containerId);
                return containerId;
            }
            catch (COMException ex)
            {
                if (ex.Message.Contains("2B72133B-3F5B-4602-8952-803546CE3344"))
                {
                    throw new Exception("Please install IIS.", ex);
                }
                throw;
            }
        }

        public void DeleteContainer(String containerId)
        {
            try
            {
                ServerManager serverManager = ServerManager.OpenRemote("localhost");
                var path = containerPathService.GetContainerRoot(containerId);

                var sitesToBeRemoved = serverManager.Sites.Where(s => s.Name == containerId).ToList();
                foreach (var site in sitesToBeRemoved)
                {
                    serverManager.Sites.Remove(site);
                }

                var appPoolName = HandleToAppPoolName(containerId);
                var appPoolsToBeRemoved = serverManager.ApplicationPools.Where(p => p.Name == appPoolName).ToList();
                foreach (var pool in appPoolsToBeRemoved)
                {
                    serverManager.ApplicationPools.Remove(pool);
                }

                containerPathService.DeleteContainerDirectory(containerId);

                serverManager.CommitChanges();
            }
            catch (COMException ex)
            {
                if (ex.Message.Contains("2B72133B-3F5B-4602-8952-803546CE3344"))
                {
                    throw new Exception("Please install IIS.", ex);
                }
                throw;
            }
        }

        public static string HandleToAppPoolName(string handle)
        {
            return string.Join("", handle.Replace("-", "").Take(64));
        }
    }
}
