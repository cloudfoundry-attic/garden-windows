﻿#region

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Containerizer.Services.Implementations;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs.Features
{
    internal class ConsumerCanPutFileSystemContentsAsTarStream : nspec
    {
        private string directoryPath;
        private string handle;
        private int port;
        private string tgzName;


        private void before_each()
        {
            port = 8088;
            Helpers.SetupSiteInIIS("Containerizer", "Containerizer.Tests", "ContainerizerTestsApplicationPool", port,
                true);
            directoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(directoryPath);
            File.WriteAllText(Path.Combine(directoryPath, "file.txt"), "stuff!!!!");
            tgzName = Guid.NewGuid().ToString();
            new TarStreamService().CreateTarFromDirectory(directoryPath, tgzName);
        }

        private void after_each()
        {
            Helpers.RemoveExistingSite("Containerizer.Tests", "ContainerizerTestsApplicationPool");
            Directory.Delete(directoryPath, true);
        }

        private void describe_stream_in()
        {
            context["given that I'm a consumer of the containerizer api"] = () =>
            {
                HttpClient client = null;

                before = () =>
                {
                    client = new HttpClient {BaseAddress = new Uri("http://localhost:" + port)};
                };

                context["there exists a container with a given id"] = () =>
                {
                    before = () =>
                    {
                        handle = Helpers.CreateContainer(client);
                    };

                    after = () => Helpers.DestroyContainer(client, handle);

                    context["when I PUT a request to /api/Containers/:id/files?destination=%2F"] = () =>
                    {
                        HttpResponseMessage responseMessage = null;
                        Stream fileStream = null;

                        before = () =>
                        {
                            var content = new MultipartFormDataContent();
                            fileStream = new FileStream(tgzName, FileMode.Open);
                            var streamContent = new StreamContent(fileStream);
                            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                            content.Add(streamContent);
                            string path = "/api/containers/" + handle + "/files?destination=%2F";
                            responseMessage = client.PutAsync(path, streamContent).GetAwaiter().GetResult();
                        };

                        it["returns a successful status code"] =
                            () =>
                            {
                                responseMessage.IsSuccessStatusCode.should_be_true();
                            };

                        it["sees the new file in the container"] = () =>
                        {
                            string fileContent = File.ReadAllText(Path.Combine(Helpers.GetContainerPath(handle), "file.txt"));
                            fileContent.should_be("stuff!!!!");
                        };

                        after = () =>
                        {
                            fileStream.Close();
                        };
                    };
                };
            };
        }
    }
}