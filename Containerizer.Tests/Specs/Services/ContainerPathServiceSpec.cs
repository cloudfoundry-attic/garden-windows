using System;
using System.IO;
using System.Reflection;
using Containerizer.Services.Implementations;
using NSpec;

namespace Containerizer.Tests.Specs.Services
{
    internal class ContainerPathServiceSpec : nspec
    {
        private ContainerPathService containerPathService;
        private string id;
        private string returnedPath;

        private void before_each()
        {
            containerPathService = new ContainerPathService();
        }

        private void describe_path_service()
        {
            string expectedPath = null;
            before = () =>
            {
                id = Guid.NewGuid().ToString();
                string rootDir =
                    Directory.GetDirectoryRoot(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                expectedPath = Path.Combine(rootDir, "containerizer", id);
            };
            describe["#GetContainerRoot"] = () =>
            {
                before = () => { returnedPath = containerPathService.GetContainerRoot(id); };

                it["returns the path to the container's root directory"] =
                    () => { returnedPath.should_be(expectedPath); };
            };

            describe["#CreateContainerDirectory"] = () =>
            {
                before = () => { containerPathService.CreateContainerDirectory(id); };
                after = () => { Directory.Delete(containerPathService.GetContainerRoot(id), true); };

                it["creates the container's root directory"] =
                    () => { Directory.Exists(expectedPath).should_be_true(); };

                after = () => { Directory.Delete(expectedPath); };
            };
        }
    }
}