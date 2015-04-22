#region

using System.IO;
using System.Reflection;
using IronFrame;

#endregion

namespace Containerizer.Factories
{
    public class ContainerServiceFactory
    {
        public IContainerService New()
        {
            return new ContainerService(GetContainerRoot(), "Users");
        }
        public static string GetContainerRoot()
        {
            string rootDir = Directory.GetDirectoryRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            return Path.Combine(rootDir, "containerizer");
        }
    }
}