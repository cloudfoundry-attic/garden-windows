#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Containerizer.Services.Implementations;
using NSpec;
using IronFoundry.Container;
using Moq;

#endregion

namespace Containerizer.Tests.Specs.Services
{
    internal class ContainerPathServiceSpec : nspec
    {
        // *** FIXME - deprecated - delete me.
        // ContainerPathService is going away, to be replaced by IContainerService and IContainer.
        private ContainerPathService containerPathService;
        private string id;
        private string returnedPath;

        private void before_each()
        {
            containerPathService = new ContainerPathService(new Mock<IContainerService>().Object);
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

            xdescribe["#ContainerIds"] = () =>
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

            xdescribe["#GetContainerRoot"] = () =>
            {
                before = () =>
                {
                    returnedPath = containerPathService.GetContainerRoot(id);
                };

                it["returns the path to the container's root directory"] =
                    () =>
                    {
                        returnedPath.should_be(expectedPath);
                    };
            };
        }
    }
}