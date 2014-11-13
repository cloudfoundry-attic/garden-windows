using System;
using System.Collections.Generic;
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
    class ConsumerCanGetFileSystemContentsAsTarStream : nspec
    {
        string id;
        HttpClient client;
        private string containerPath;

        void before_each()
        {
            var port = 8088;
            Helpers.SetupSiteInIIS("Containerizer", "Containerizer.Tests", "ContainerizerTestsApplicationPool", port, true);

            client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:" + port.ToString());
            var postTask = client.PostAsync("/api/Containers", new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()));
            postTask.Wait();
            var postResult = postTask.Result;
            var readTask = postResult.Content.ReadAsStringAsync();
            readTask.Wait();
            var response = readTask.Result;
            var json = JObject.Parse(response);
            id = json["id"].ToString();
            containerPath = new ContainerPathService().GetContainerRoot(id);
            File.WriteAllText(Path.Combine(containerPath, "file.txt"), "stuff!!!!");
        }

        void after_each()
        {
            Helpers.RemoveExistingSite("Containerizer.Tests", "ContainerizerTestsApplicationPool");
            Helpers.RemoveExistingSite(id, id);
            Directory.Delete(containerPath, true);
        }

        void describe_stream_out()
        {
            context["when asking for a file"] = () =>
            {
                it["streams the file as a tarball"] = () =>
                {
                    var getTask = client.GetAsync("/api/containers/" + id + "/files/file.txt").GetAwaiter().GetResult();
                    getTask.IsSuccessStatusCode.should_be_true();
                    var tgzStream = getTask.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                    using (var tgz = ReaderFactory.Open(tgzStream))
                    {
                        tgz.MoveToNextEntry().should_be_true();
                        tgz.Entry.Key.should_be("file.txt");

                        tgz.MoveToNextEntry().should_be_false();
                    }
                };
            };

            context["when asking for a directory"] = () =>
            {
            };

            context["when tar fails"] = () =>
            {
            };
        }
    }
}


