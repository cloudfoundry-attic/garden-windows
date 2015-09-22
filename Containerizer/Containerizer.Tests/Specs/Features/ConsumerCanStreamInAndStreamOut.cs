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
        private string handle;
        private Helpers.ContainerizerProcess process;
        private string tgzPath;

        private void before_each()
        {
            process = Helpers.CreateContainerizerProcess();
            tgzPath = Helpers.CreateTarFile();
        }

        private void after_each()
        {
            var tgzDirectory = Directory.GetParent(tgzPath);
            tgzDirectory.Delete(true);
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

                        before = () =>
                        {
                            responseMessage = Helpers.StreamIn(handle: handle, tgzPath: tgzPath, client: client);
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
