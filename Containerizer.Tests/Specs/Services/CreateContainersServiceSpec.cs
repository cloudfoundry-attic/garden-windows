using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Containerizer.Services.Implementations;
using Microsoft.Web.Administration;
using NSpec;

namespace Containerizer.Tests.Specs.Services
{
    internal class CreateContainersServiceSpec : nspec
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
                    passedInId = Guid.NewGuid().ToString();
                    var createContainerService = new CreateContainerService();
                    returnedId = createContainerService.CreateContainer(passedInId).Result;
                    createContainerService = new CreateContainerService();
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
                it["creates a new associated app pool in IIS named with the passed in id"] =
                    () =>
                    {
                        serverManager.Sites.First(x => x.Name == returnedId).Applications[0].ApplicationPoolName
                            .should_be(passedInId);
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
                        string rootDir =
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
        }
    }
}