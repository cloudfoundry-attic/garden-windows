#region

using System.IO;
using System.Reflection;
using IronFrame;
using Containerizer.Services.Interfaces;

#endregion

namespace Containerizer.Factories
{
    public class ContainerServiceFactory
    {
        private readonly string containerDirectory;

        public ContainerServiceFactory(IOptions options)
        {
            containerDirectory = options.ContainerDirectory;
        }

        public IContainerService New()
        {
            return new ContainerService(containerDirectory, "Users");
        }

        public static string GetContainerDefaultRoot()
        {
            string rootDir = Directory.GetDirectoryRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            return Path.Combine(rootDir, "containerizer");
        }
    }
}