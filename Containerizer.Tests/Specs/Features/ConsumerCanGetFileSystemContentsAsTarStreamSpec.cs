#region

using System;
using System.IO;
using System.Net.Http;
using Containerizer.Services.Implementations;
using NSpec;
using SharpCompress.Reader;

#endregion

namespace Containerizer.Tests.Specs.Features
{
    internal class ConsumerCanGetFileSystemContentsAsTarStreamSpec : nspec
    {
        private HttpClient client;
        private string containerPath;
        private string id;

        private void before_each()
        {
            const int port = 8088;
            Helpers.SetupSiteInIIS("Containerizer", "Containerizer.Tests", "ContainerizerTestsApplicationPool", port,
                true);

            client = new HttpClient {BaseAddress = new Uri("http://localhost:" + port)};
            id = Helpers.CreateContainer(client);
            containerPath = new ContainerPathService().GetContainerRoot(id);
            File.WriteAllText(Path.Combine(containerPath, "file.txt"), "stuff!!!!");
        }

        private void after_each()
        {
            Helpers.RemoveExistingSite("Containerizer.Tests", "ContainerizerTestsApplicationPool");
            Helpers.RemoveExistingSite(id, id);
            Directory.Delete(containerPath, true);
        }

        private void describe_stream_out()
        {
            context["when asking for a file"] = () =>
            {
                it["streams the file as a tarball"] = () =>
                {
                    HttpResponseMessage getTask =
                        client.GetAsync("/api/containers/" + id + "/files?source=/file.txt").GetAwaiter().GetResult();
                    getTask.IsSuccessStatusCode.should_be_true();
                    Stream tarStream = getTask.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                    using (IReader tar = ReaderFactory.Open(tarStream))
                    {
                        tar.MoveToNextEntry().should_be_true();
                        tar.Entry.Key.should_be("file.txt");

                        tar.MoveToNextEntry().should_be_false();
                    }
                };
            };
        }
    }
}