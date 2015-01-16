#region

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Containerizer.Services.Implementations;
using Microsoft.Web.Administration;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs.Services
{
    internal class ContainerServiceSpec : nspec
    {
        private void describe_()
        {
            describe["#CreateContainer"] = () =>
            {
                string passedInId = null;
                string returnedId = null;
                ServerManager serverManager = null;

                before = () =>
                {
                    passedInId = Guid.NewGuid() + "-" + Guid.NewGuid();
                    var containerService = new ContainerService();
                    returnedId = containerService.CreateContainer(passedInId);
                    containerService = new ContainerService();
                    serverManager = ServerManager.OpenRemote("localhost");
                };

                after = () =>
                {
                    Directory.Delete(new ContainerPathService().GetContainerRoot(returnedId), true);
                    Helpers.RemoveExistingSite(passedInId, passedInId);
                };

                it["creates a new site in IIS named with the given id"] =
                    () => { serverManager.Sites.should_contain(x => x.Name == returnedId); };

                it["returns the passed in id"] =
                    () => { returnedId.should_be(passedInId); };
                it["creates a new associated app pool in IIS named with the truncated passed in id"] =
                    () =>
                    {
                        serverManager.Sites.First(x => x.Name == returnedId).Applications[0].ApplicationPoolName
                            .should_be(passedInId.Replace("-", "").Substring(0, 64));
                    };

                it["creates a site with exactly one application"] =
                    () => { serverManager.Sites.First(x => x.Name == returnedId).Applications.Count.should_be(1); };

                it["creates a site with an application mapped to the root virtual directory"] =
                    () =>
                    {
                        serverManager.Sites.First(x => x.Name == returnedId).Applications[0].VirtualDirectories[0].Path
                            .should_be(
                                "/");
                    };

                describe["folder operations"] = () =>
                {
                    string expectedPath = null;
                    before = () =>
                    {
                        var rootDir =
                            Directory.GetDirectoryRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                        expectedPath = Path.Combine(rootDir, "containerizer", returnedId);
                    };

                    it[
                        @"creates a folder on the file system in the folder 'containerizer' in the root directory (e.g. C:\containerizer\38272f66-ae9c-4369-bfe4-4d8a1152c1ce)"
                        ] = () => { Directory.Exists(expectedPath).should_be_true(); };

                    it[
                        @"associates the site with a folder in the folder 'containerizer' the root directory (e.g. C:\containerizer\38272f66-ae9c-4369-bfe4-4d8a1152c1ce)"
                        ] = () =>
                        {
                            // C:\\ on Windows
                            serverManager.Sites.First(x => x.Name == returnedId).Applications[0].VirtualDirectories["/"]
                                .PhysicalPath.should_be(expectedPath);
                        };
                };
            };

            describe["#DeleteContainer"] = () =>
            {
                string handle = null;
                string appPoolName = null;
                string path = null;
                ServerManager serverManager = null;

                before = () =>
                {
                    handle = Guid.NewGuid() + "-" + Guid.NewGuid();
                    appPoolName = Guid.NewGuid().ToString();
                    path = new ContainerPathService().GetContainerRoot(handle);
                    Directory.CreateDirectory(path);
                    Helpers.SetupSiteInIIS(path, handle, appPoolName, 3333, false);

                    var containerService = new ContainerService();
                    serverManager = ServerManager.OpenRemote("localhost");
                    serverManager.Sites.should_contain(x => x.Name == handle);
                    serverManager.ApplicationPools.should_contain(x => x.Name == appPoolName);
                    Directory.Exists(path).should_be_true();

                    containerService.DeleteContainer(handle);

                    serverManager = ServerManager.OpenRemote("localhost"); // Refresh
                };

                after = () => Directory.Delete(path, true);

                it["destroys the site in IIS with the given name"] =
                    () => { serverManager.Sites.should_not_contain(x => x.Name == handle); };

                it["destroys the app pool in IIS with the given name"] =
                    () => { serverManager.ApplicationPools.should_not_contain(x => x.Name == appPoolName); };
            };
        }
    }
}