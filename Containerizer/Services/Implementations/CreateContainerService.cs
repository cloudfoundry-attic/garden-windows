using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Containerizer.Services.Interfaces;
using Microsoft.Web.Administration;
using System.Linq;

namespace Containerizer.Services.Implementations
{
    public class CreateContainerService : ICreateContainerService
    {
        public Task<string> CreateContainer(String containerId)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    ServerManager serverManager = ServerManager.OpenRemote("localhost");
                    string rootDir =
                        Directory.GetDirectoryRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                    string path = Path.Combine(rootDir, "containerizer", containerId);
                    var appPoolName = string.Join("", containerId.Replace("-", "").Take(64));
                    Site site = serverManager.Sites.Add(containerId, path, 0);

                    serverManager.ApplicationPools.Add(appPoolName);
                    site.Applications[0].ApplicationPoolName = appPoolName;
                    ApplicationPool appPool = serverManager.ApplicationPools[appPoolName];
                    appPool.ManagedPipelineMode = ManagedPipelineMode.Integrated;

                    serverManager.CommitChanges();
                    Directory.CreateDirectory(path);
                    return containerId;
                }
                catch (COMException ex)
                {
                    if (ex.Message.Contains("2B72133B-3F5B-4602-8952-803546CE3344"))
                    {
                        throw new CouldNotCreateContainerException("Please install IIS.", ex);
                    }
                    throw new CouldNotCreateContainerException("", ex);
                }
                catch (Exception ex)
                {
                    throw new CouldNotCreateContainerException(String.Empty, ex);
                }
            });
        }
    }
}