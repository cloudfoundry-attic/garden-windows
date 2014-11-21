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
        private CreateContainerService createContainerService;
        private string id;
        private ServerManager serverManager;

        private void before_each()
        {
            createContainerService = new CreateContainerService();
            serverManager = new ServerManager();
        }

        private void describe_create_container()
        {
            before = () =>
            {
                Task<string> task = createContainerService.CreateContainer();
                task.Wait();
                id = task.Result;
            };

            it["creates a new site in IIS named with the given id"] =
                () => { serverManager.Sites.should_contain(x => x.Name == id); };
            it["creates a new associated app pool in IIS named with the given id"] =
                () =>
                {
                    serverManager.Sites.First(x => x.Name == id).Applications[0].ApplicationPoolName.should_be(id);
                };

            it["creates a site with exactly one application"] =
                () => { serverManager.Sites.First(x => x.Name == id).Applications.Count.should_be(1); };

            it["creates a site with an application mapped to the root virtual directory"] =
                () =>
                {
                    serverManager.Sites.First(x => x.Name == id).Applications[0].VirtualDirectories[0].Path.should_be(
                        "/");
                };

            describe["folder operations"] = () =>
            {
                string expectedPath = null;
                before = () =>
                {
                    string rootDir =
                        Directory.GetDirectoryRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                    expectedPath = Path.Combine(rootDir, "containerizer", id);
                };

                it[
                    @"creates a folder on the file system in the folder 'containerizer' in the root directory (e.g. C:\containerizer\38272f66-ae9c-4369-bfe4-4d8a1152c1ce)"
                    ] = () => { Directory.Exists(expectedPath).should_be_true(); };

                it[
                    @"associates the site with a folder in the folder 'containerizer' the root directory (e.g. C:\containerizer\38272f66-ae9c-4369-bfe4-4d8a1152c1ce)"
                    ] = () =>
                    {
                        // C:\\ on Windows
                        serverManager.Sites.First(x => x.Name == id).Applications[0].VirtualDirectories["/"]
                            .PhysicalPath.should_be(expectedPath);
                    };
            };
        }

        private void after_each()
        {
            Helpers.RemoveExistingSite(id, id);
        }
    }
}