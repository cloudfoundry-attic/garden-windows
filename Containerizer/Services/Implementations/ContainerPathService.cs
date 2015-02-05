#region

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Containerizer.Services.Interfaces;
using IronFoundry.Container;

#endregion

namespace Containerizer.Services.Implementations
{
    public class ContainerPathService : IContainerPathService
    {
        private IContainerService containerService;
        public ContainerPathService(IContainerService containerService)
        {
            this.containerService = containerService;
        }

        public string GetContainerRoot(string id)
        {
            return containerService.GetContainerByHandle(id).Directory.MapUserPath("");
        }

        public string GetSubdirectory(string id, string destination)
        {
            return containerService.GetContainerByHandle(id).Directory.MapUserPath(destination);
        }

        public static string GetContainerRoot()
        {
            string rootDir = Directory.GetDirectoryRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            return Path.Combine(rootDir, "containerizer");
        }
    }
}