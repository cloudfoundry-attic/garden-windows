#region

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Containerizer.Services.Implementations;
using NSpec;
using SharpCompress.Reader;

#endregion

namespace Containerizer.Tests.Specs.Features
{
    internal class ConsumerCanStreamInAndStreamOut : nspec
    {
        private string directoryPath;
        private string handle;
        private string tgzName;
        private Helpers.ContainarizerProcess process;


        private void before_each()
        {
            process = Helpers.CreateContainerizerProcess();
            CreateTarToSend();
        }

        private void CreateTarToSend()
        {
            directoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            Directory.CreateDirectory(directoryPath);
            File.WriteAllText(Path.Combine(directoryPath, "file.txt"), "stuff!!!!");
            tgzName = Guid.NewGuid().ToString();
            new TarStreamService().CreateTarFromDirectory(directoryPath, tgzName);
        }

        private void after_each()
        {
            Directory.Delete(directoryPath, true);
            process.Dispose();
        }

        private void describe_stream_in()
        {
            context["given that I'm a consumer of the containerizer api"] = () =>
            {
                HttpClient client = null;

                before = () => client = process.GetClient();

                context["there exists a container with a given id"] = () =>
                {
                    before = () => handle = Helpers.CreateContainer(client);
                    after = () => Helpers.DestroyContainer(client, handle);

                    context["when I stream in a file into the container"] = () =>
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

                        after = () =>
                        {
                            fileStream.Close();
                        };

                        it["returns a successful status code"] = () =>
                        {
                            responseMessage.IsSuccessStatusCode.should_be_true();
                        };

                        context["and I stream the file out of the container"] = () =>
                        {
                            it["returns a tarred version of the file"] = () =>
                            {

                                responseMessage.IsSuccessStatusCode.should_be_true();

                                HttpResponseMessage getTask =
                                    client.GetAsync("/api/containers/" + handle + "/files?source=/file.txt")
                                        .GetAwaiter()
                                        .GetResult();
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
                    };
                };
            };
        }
    }
}
