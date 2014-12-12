using System;
using System.Collections.Generic;
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
            string rootDir = null;
            string expectedPath = null;
            before = () =>
            {
                id = Guid.NewGuid().ToString();
                rootDir =
                    Directory.GetDirectoryRoot(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                expectedPath = Path.Combine(rootDir, "containerizer", id);
            };

            describe["#ContainerIds"] = () =>
            {
                before = () =>
                {
                    string rootPath = containerPathService.GetContainerRoot(".");
                    Directory.CreateDirectory(Path.Combine(rootPath, "MyFirstContainer"));
                    Directory.CreateDirectory(Path.Combine(rootPath, "MySecondContainer"));
                };
                after = () =>
                {
                    string rootPath = containerPathService.GetContainerRoot(".");
                    Directory.Delete(Path.Combine(rootPath, "MyFirstContainer"), true);
                    Directory.Delete(Path.Combine(rootPath, "MySecondContainer"), true);
                };

                it["returns all the container ids"] = () =>
                {
                    IEnumerable<string> ids = containerPathService.ContainerIds();
                    ids.should_contain("MyFirstContainer");
                    ids.should_contain("MySecondContainer");
                };
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