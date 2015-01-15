#region

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Containerizer.Services.Interfaces;

#endregion

namespace Containerizer.Services.Implementations
{
    public class ContainerPathService : IContainerPathService
    {
        public string GetContainerRoot(string id)
        {
            return Path.Combine(GetContainerRoot(), id);
        }

        public void CreateContainerDirectory(string id)
        {
            Directory.CreateDirectory(GetContainerRoot(id));
        }

        public void DeleteContainerDirectory(string id)
        {
            Directory.Delete(GetContainerRoot(id));
        }

        public string GetSubdirectory(string id, string destination)
        {
            return Path.GetFullPath(GetContainerRoot(id) + destination);
        }

        public IEnumerable<string> ContainerIds()
        {
            string[] dirs = Directory.GetDirectories(GetContainerRoot());
            return dirs.Select(Path.GetFileName);
        }

        private string GetContainerRoot()
        {
            string rootDir = Directory.GetDirectoryRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            return Path.Combine(rootDir, "containerizer");
        }
    }
}