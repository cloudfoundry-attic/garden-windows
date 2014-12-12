using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSpec;
using Containerizer.Tests.Specs;

namespace Containerizer.Tests.Specs.Controllers
{
    internal class ContainersControllerSpec : nspec
    {
        private ContainersController containersController;
        private Mock<IContainerPathService> mockContainerPathService;
        private Mock<ICreateContainerService> mockCreateContainerService;
        private Mock<IStreamInService> mockStreamInService;
        private Mock<IStreamOutService> mockStreamOutService;
        private Mock<INetInService> mockNetInService;

        private void before_each()
        {
            mockContainerPathService = new Mock<IContainerPathService>();
            mockCreateContainerService = new Mock<ICreateContainerService>();
            mockStreamOutService = new Mock<IStreamOutService>();
            mockStreamInService = new Mock<IStreamInService>();
            mockNetInService = new Mock<INetInService>();
            containersController = new ContainersController(mockContainerPathService.Object,
                mockCreateContainerService.Object, mockStreamInService.Object,
                mockStreamOutService.Object, mockNetInService.Object)
            {
                Configuration = new HttpConfiguration(),
                Request = new HttpRequestMessage()
            };
        }

        private
        void describe_list()
        {
            HttpResponseMessage result = null;

                before = () =>
                {
                    mockContainerPathService.Setup(x => x.ContainerIds()).Returns(new List<string>{ {"MyFirstContainer"}, {"MySecondContainer"}  });
                   result = containersController.List()
                       .GetAwaiter()
                       .GetResult()
                       .ExecuteAsync(new CancellationToken())
                       .GetAwaiter()
                       .GetResult();
                };

                it["returns a successful status code"] = () =>
                {
                    result.IsSuccessStatusCode.should_be_true();
                };

                it["returns a list of container ids as strings"] = () =>
                {
                    var jsonString = result.Content.ReadAsString(); // Json();
                    var json = JsonConvert.DeserializeObject<string[]>(jsonString);
                    json.should_contain("MyFirstContainer");
                    json.should_contain("MySecondContainer");
                };
            
        }

        private void describe_post()
        {
            context["when the container is created successfully"] = () =>
            {
                string containerId = null;

                before = () =>
                {
                    containerId = Guid.NewGuid().ToString();
                    mockCreateContainerService.Setup(x => x.CreateContainer(It.IsAny<String>())).Returns((String x) => Task.FromResult(x));
                    containersController.Request.Content = new StringContent("{Handle: \"" + containerId + "\"}");
                };

                it["returns a successful status code"] = () =>
                {
                    containersController.Post().Result
                        .ExecuteAsync(new CancellationToken()).Result
                        .IsSuccessStatusCode.should_be_true();
                };

                it["returns the passed in container's id"] = () =>
                {
                    Task<IHttpActionResult> postTask = containersController.Post();
                    postTask.Wait();
                    Task<HttpResponseMessage> resultTask = postTask.Result.ExecuteAsync(new CancellationToken());
                    resultTask.Wait();
                    Task<string> readTask = resultTask.Result.Content.ReadAsStringAsync();
                    readTask.Wait();
                    JObject json = JObject.Parse(readTask.Result);
                    json["id"].ToString().should_be(containerId);
                };
            };
            context["when the container is not created successfully"] = () =>
            {
                before =
                    () =>
                    {
                        mockCreateContainerService.Setup(x => x.CreateContainer(It.IsAny<String>()))
                            .ThrowsAsync(new CouldNotCreateContainerException(String.Empty, null));
                        containersController.Request.Content = new StringContent("{Handle: \"guid\"}");
                    };

                it["returns a error status code"] = () =>
                {
                    Task<IHttpActionResult> postTask = containersController.Post();
                    postTask.Wait();
                    Task<HttpResponseMessage> resultTask = postTask.Result.ExecuteAsync(new CancellationToken());
                    resultTask.Wait();
                    resultTask.Result.IsSuccessStatusCode.should_be_false();
                };
            };
        }

        private void describe_get_files()
        {
            context["when the file exists"] = () =>
            {
                HttpResponseMessage result = null;
                before = () =>
                {
                    mockStreamOutService.Setup(x => x.StreamOutFile(It.IsAny<string>(), It.IsAny<string>()))
                        .Returns(() =>
                        {
                            var stream = new MemoryStream();
                            var writer = new StreamWriter(stream);
                            writer.Write("hello");
                            writer.Flush();
                            stream.Position = 0;
                            return stream;
                        });

                    result = containersController
                        .StreamOut("guid", "file.txt").GetAwaiter().GetResult();
                };


                it["returns a successful status code"] = () => { result.IsSuccessStatusCode.should_be_true(); };
            };
        }

        private void describe_put_files()
        {
            context["when it receives a new file"] = () =>
            {
                string id = null;
                const string fileName = "file.txt";
                HttpResponseMessage result = null;
                string content = null;

                before = () =>
                {
                    id = Guid.NewGuid().ToString();
                    Stream stream = new MemoryStream();
                    var sr = new StreamWriter(stream);
                    content = Guid.NewGuid().ToString();
                    sr.Write(content);
                    sr.Flush();
                    stream.Seek(0, SeekOrigin.Begin);
                    containersController.Request.Content = new StreamContent(stream);
                    result = containersController.StreamIn(id, fileName).GetAwaiter().GetResult();
                };

                it["calls the stream in service with the correct stream, passed in id, and file name query parameter"] =
                    () =>
                    {
                        mockStreamInService.Verify(x => x.StreamInFile(
                            It.Is((Stream y) => new StreamReader(y).ReadToEnd() == content),
                            id,
                            fileName));
                    };


                it["returns a successful status code"] = () => { result.IsSuccessStatusCode.should_be_true(); };
            };
        }

        private void describe_net_in()
        {
            string containerId = null;
            int requestedContainerPort = 0;
            HttpResponseMessage result = null;

            before = () =>
            {
                containerId = Guid.NewGuid().ToString();
                requestedContainerPort = 5432;
            };

            act = () =>
            {
                containersController.Request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string,string>>
                {
                    new KeyValuePair<string, string>("hostPort", requestedContainerPort.ToString())
                });

                result =
                    containersController.NetIn(containerId)
                        .GetAwaiter()
                        .GetResult()
                        .ExecuteAsync(new CancellationToken())
                        .GetAwaiter()
                        .GetResult();
            };

            it["returns a successful status code"] = () => { result.IsSuccessStatusCode.should_be_true(); };

            it["calls the net in service with its passed in parameters"] = () =>
            {
                mockNetInService.Verify(x => x.AddPort(requestedContainerPort, containerId));
            };

            context["net in service add port succeeds and returns a port"] = () =>
            {
                before = () =>
                {
                    mockNetInService.Setup(x => x.AddPort(It.IsAny<int>(), It.IsAny<string>())).Returns(8765);
                };

                it["returns the port that the net in service returns"] = () =>
                {
                    JObject json = result.Content.ReadAsJson();
                    json["hostPort"].ToObject<int>().should_be(8765);
                };
            };
        }
    }
}