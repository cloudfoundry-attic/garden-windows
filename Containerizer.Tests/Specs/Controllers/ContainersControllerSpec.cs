using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSpec;
using Containerizer.Tests.Specs;
using Microsoft.Web.Administration;

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
        private Mock<IMetadataService> mockMetadataService;

        private void before_each()
        {
            mockContainerPathService = new Mock<IContainerPathService>();
            mockCreateContainerService = new Mock<ICreateContainerService>();
            mockStreamOutService = new Mock<IStreamOutService>();
            mockStreamInService = new Mock<IStreamInService>();
            mockNetInService = new Mock<INetInService>();
            mockMetadataService = new Mock<IMetadataService>();
            containersController = new ContainersController(mockContainerPathService.Object,
                mockCreateContainerService.Object, mockStreamInService.Object,
                mockStreamOutService.Object, mockNetInService.Object, mockMetadataService.Object)
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
                mockContainerPathService.Setup(x => x.ContainerIds()).Returns(new List<string> { { "MyFirstContainer" }, { "MySecondContainer" } });
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
                string containerHandle = null;
                Dictionary<string, string> properties = null;
                string key = null;
                string value = null;
                
                before = () =>
                {
                    containerHandle = Guid.NewGuid().ToString();
                    key = "hiwillyou";
                    value = "bemyfriend";
                    properties = new Dictionary<string, string>()
                    {
                        {key, value}
                    };

                    mockCreateContainerService.Setup(x => x.CreateContainer(It.IsAny<String>())).Returns((String x) => Task.FromResult(x));
                    containersController.Request.Content = 
                        new StringContent("{Handle: \"" + containerHandle + "\", Properties:{\"" + key + "\": \"" + value + "\"}}");
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
                    json["id"].ToString().should_be(containerHandle);
                };

                it["sets metadata"] = () =>
                {
                    containersController.Post().Wait();
                    mockMetadataService.Verify(x => x.BulkSetMetadata(containerHandle, It.Is((Dictionary<string,string> y) => y[key] == value)));
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

            context["when metadata properties are not passed to the endpoint"] = () =>
            {
                before = () =>
                {
                    string containerHandle = null;
                    containerHandle = Guid.NewGuid().ToString();

                    mockCreateContainerService.Setup(x => x.CreateContainer(It.IsAny<String>())).Returns((String x) => Task.FromResult(x));
                    containersController.Request.Content = 
                        new StringContent("{Handle: \"" + containerHandle + "\"}");
                };

                it["returns a successful status code"] = () =>
                {
                    containersController.Post().Result
                        .ExecuteAsync(new CancellationToken()).Result
                        .IsSuccessStatusCode.should_be_true();
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
                containersController.Request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
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

        private void describe_get_property()
        {
            string containerId = null;
            IHttpActionResult result = null;
            string propertyValue = null;

            before = () =>
            {
                containerId = Guid.NewGuid().ToString();
                propertyValue = "a lion, a hippo, the number 25";
                mockMetadataService.Setup(x => x.GetMetadata(It.IsIn(new []{containerId}), It.IsIn(new []{"key"})))
                    .Returns(() =>
                    {
                        return propertyValue;
                    });

                result = containersController
                    .GetProperty(containerId, "key").GetAwaiter().GetResult();
            };


            it["returns a successful status code"] = () =>
            {
                result.ExecuteAsync(new CancellationToken()).Result.IsSuccessStatusCode.should_be_true();
            };

            it["returns the correct property value"] = () =>
            {
                result.ExecuteAsync(new CancellationToken()).Result.Content.ReadAsJson()["value"].ToString().should_be(
                    propertyValue);
            };
        }
    }
}