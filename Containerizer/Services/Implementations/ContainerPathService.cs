using System.IO;
using System.Reflection;
using Containerizer.Services.Interfaces;

namespace Containerizer.Services.Implementations
{
    public class ContainerPathService : IContainerPathService
    {
        public string GetContainerRoot(string id)
        {
            var rootDir = Directory.GetDirectoryRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            return Path.Combine(rootDir, "containerizer", id);
        }


        public void CreateContainerDirectory(string id)
        {
            Directory.CreateDirectory(GetContainerRoot(id));
        }

        public string GetSubdirectory(string id, string destination)
        {
            return Path.GetFullPath(GetContainerRoot(id) + destination);
        }
    }
}