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
    class CreateContainersServiceSpec : nspec
    {
        CreateContainerService createContainerService;
        ServerManager serverManager;
        string id;

        void before_each()
        {
            createContainerService = new CreateContainerService();
            serverManager = new ServerManager();
        }

        void describe_create_container()
        {
            before = () =>
            {
                var task = createContainerService.CreateContainer();
                task.Wait();
                id = task.Result;
            };

            it["creates a new site in IIS named with the given id"] = () =>
            {
                serverManager.Sites.should_contain(x => x.Name == id);
            };
            it["creates a new associated app pool in IIS named with the given id"] = () =>
            {
                serverManager.Sites.First(x => x.Name == id).Applications[0].ApplicationPoolName.should_be(id);
            };

            it["creates a site with exactly one application"] = () =>
            {
                serverManager.Sites.First(x => x.Name == id).Applications.Count.should_be(1);
            };

            it["creates a site with an application mapped to the root virtual directory"] = () =>
            {
                serverManager.Sites.First(x => x.Name == id).Applications[0].VirtualDirectories[0].Path.should_be("/");
            };

            describe["folder operations"] = () =>
            {
                string expectedPath = null;
                before = () =>
                    {
                        var rootDir = Directory.GetDirectoryRoot(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                        expectedPath = Path.Combine(rootDir, "containerizer", id);
                    };

                it[@"creates a folder on the file system in the folder 'containerizer' in the root directory (e.g. C:\containerizer\38272f66-ae9c-4369-bfe4-4d8a1152c1ce)"] = () =>
                {
                    System.IO.Directory.Exists(expectedPath).should_be_true();
                };

                it[@"associates the site with a folder in the folder 'containerizer' the root directory (e.g. C:\containerizer\38272f66-ae9c-4369-bfe4-4d8a1152c1ce)"] = () =>
                {
                    // C:\\ on Windows
                    serverManager.Sites.First(x => x.Name == id).Applications[0].VirtualDirectories["/"].PhysicalPath.should_be(expectedPath);
                };
            };
        }

        void after_each()
        {
            Helpers.RemoveExistingSite(id, id);
        }

    }
}


