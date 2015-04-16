#region

using System;
using System.IO;
using System.Net.Http;
using Containerizer.Services.Implementations;
using NSpec;
using SharpCompress.Reader;
using IronFrame;

#endregion

namespace Containerizer.Tests.Specs.Features
{
    internal class ConsumerCanGetFileSystemContentsAsTarStreamSpec : nspec
    {
        private HttpClient client;
        private string handle;

        private void before_each()
        {
            const int port = 8088;
            Helpers.SetupSiteInIIS("Containerizer", "Containerizer.Tests", "ContainerizerTestsApplicationPool", port,
                true);

            client = new HttpClient {BaseAddress = new Uri("http://localhost:" + port)};
            handle = Helpers.CreateContainer(client);
            var containerPath = Helpers.GetContainerPath(handle);
            File.WriteAllText(Path.Combine(containerPath, "file.txt"), "stuff!!!!");
        }

        private void after_each()
        {
            Helpers.DestroyContainer(client, handle);
            Helpers.RemoveExistingSite("Containerizer.Tests", "ContainerizerTestsApplicationPool");
        }

        private void describe_stream_out()
        {
            context["when asking for a file"] = () =>
            {
                it["streams the file as a tarball"] = () =>
                {
                    HttpResponseMessage getTask =
                        client.GetAsync("/api/containers/" + handle + "/files?source=/file.txt").GetAwaiter().GetResult();
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