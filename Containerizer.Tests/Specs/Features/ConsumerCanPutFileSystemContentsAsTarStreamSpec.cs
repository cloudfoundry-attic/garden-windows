using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using Containerizer.Services.Implementations;
using NSpec;
using System.Linq;
using System.Web.Http.Results;
using System.IO;
using Microsoft.Web.Administration;
using System.Net.Http;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;
using SharpCompress.Reader;

namespace Containerizer.Tests
{

    class ConsumerCanPutFileSystemContentsAsTarStream : nspec
    {
        string id;
        HttpClient client;
        private int port;
        private string directoryPath;
        private string tgzName;

        void before_each()
        {
            port = 8088;
            Helpers.SetupSiteInIIS("Containerizer", "Containerizer.Tests", "ContainerizerTestsApplicationPool", port, true);
            directoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(directoryPath);
            File.WriteAllText(Path.Combine(directoryPath, "file.txt"), "stuff!!!!");
            tgzName = Guid.NewGuid().ToString();
            new TarStreamService().CreateFromDirectory(directoryPath, tgzName);
        }

        void after_each()
        {
            Helpers.RemoveExistingSite("Containerizer.Tests", "ContainerizerTestsApplicationPool");
            Helpers.RemoveExistingSite(id, id);
            Directory.Delete(directoryPath, true);
            Directory.Delete(new ContainerPathService().GetContainerRoot(id), true);
            
        }

        void describe_stream_in()
        {
            context["given that I'm a consumer of the containerizer api"] = () =>
            {
                HttpClient client = null;

                before = () =>
                {
                    client = new HttpClient();
                    client.BaseAddress = new Uri("http://localhost:" + port.ToString());
                };

                context["there exists a container with a given id"] = () =>
                {
                    before = () =>
                    {
                        id = Helpers.CreateContainer(port);
                    };

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
                            var path = "/api/containers/" + id + "/files?destination=%2F";
                            responseMessage = client.PutAsync(path, streamContent).GetAwaiter().GetResult();
                            var x = 1;
                        };

                        it["returns a successful status code"] = () =>
                        {
                            responseMessage.IsSuccessStatusCode.should_be_true();
                        };

                        it["sees the new file in the container"] = () =>
                        {
                            var fileContent = File.ReadAllText(Path.Combine(new ContainerPathService().GetContainerRoot(id), "file.txt"));
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


