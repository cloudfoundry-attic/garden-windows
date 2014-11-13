using System;
using System.Collections.Generic;
using NSpec;
using System.Linq;
using System.Web.Http.Results;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json.Linq;
using Containerizer.Services.Interfaces;
using Containerizer.Services.Implementations;
using Moq;
using Microsoft.Web.Administration;
using System.IO;

namespace Containerizer.Tests
{
    class ContainerPathServiceSpec : nspec
    {
        ContainerPathService containerPathService;
        string id;
        string returnedPath;

        void before_each()
        {
            containerPathService = new ContainerPathService();
        }

        void describe_path_service()
        {
            string expectedPath = null;
            before = () =>
            {
                id = Guid.NewGuid().ToString();
                var rootDir =
                    Directory.GetDirectoryRoot(
                        Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                expectedPath = Path.Combine(rootDir, "containerizer", id);
            };
            describe["#GetContainerRoot"] = () =>
            {
                before = () =>
                {
                    returnedPath = containerPathService.GetContainerRoot(id);
                };

                it["returns the path to the container's root directory"] = () =>
                {
                    returnedPath.should_be(expectedPath);
                };
            };

            describe["#CreateContainerDirectory"] = () =>
            {
                before = () =>
                {
                    containerPathService.CreateContainerDirectory(id);
                };

                it["creates the container's root directory"] = () =>
                {
                    System.IO.Directory.Exists(expectedPath).should_be_true();
                };

                after = () =>
                {
                    Directory.Delete(expectedPath);
                };
            };
        }
    }
}


