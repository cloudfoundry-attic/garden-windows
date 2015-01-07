#region

using System;
using System.Collections.Generic;
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

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class ContainersControllerSpec : nspec
    {
        private ContainersController containersController;
        private Mock<IContainerPathService> mockContainerPathService;
        private Mock<ICreateContainerService> mockCreateContainerService;
        private Mock<IPropertyService> mockPropertyService;

        private void before_each()
        {
            mockContainerPathService = new Mock<IContainerPathService>();
            mockCreateContainerService = new Mock<ICreateContainerService>();
            mockPropertyService = new Mock<IPropertyService>();
            containersController = new ContainersController(mockContainerPathService.Object,
                mockCreateContainerService.Object, mockPropertyService.Object)
            {
                Configuration = new HttpConfiguration(),
                Request = new HttpRequestMessage()
            };
        }

        private void describe_list()
        {
            HttpResponseMessage result = null;

            before = () =>
            {
                mockContainerPathService.Setup(x => x.ContainerIds())
                    .Returns(new List<string>
                    {
                        "MyFirstContainer",
                        "MySecondContainer"
                    });
                result = containersController.Index()
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
                    properties = new Dictionary<string, string>
                    {
                        {key, value}
                    };

                    mockCreateContainerService.Setup(x => x.CreateContainer(It.IsAny<String>()))
                        .Returns((String x) => Task.FromResult(x));
                    containersController.Request.Content =
                        new StringContent("{Handle: \"" + containerHandle + "\", Properties:{\"" + key + "\": \"" +
                                          value + "\"}}");
                };

                it["returns a successful status code"] = () =>
                {
                    containersController.Create().Result
                        .ExecuteAsync(new CancellationToken()).Result
                        .IsSuccessStatusCode.should_be_true();
                };

                it["returns the passed in container's id"] = () =>
                {
                    Task<IHttpActionResult> postTask = containersController.Create();
                    postTask.Wait();
                    Task<HttpResponseMessage> resultTask = postTask.Result.ExecuteAsync(new CancellationToken());
                    resultTask.Wait();
                    Task<string> readTask = resultTask.Result.Content.ReadAsStringAsync();
                    readTask.Wait();
                    JObject json = JObject.Parse(readTask.Result);
                    json["id"].ToString().should_be(containerHandle);
                };

                it["sets properties"] = () =>
                {
                    containersController.Create().Wait();
                    mockPropertyService.Verify(
                        x =>
                            x.BulkSet(containerHandle, It.Is((Dictionary<string, string> y) => y[key] == value)));
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
                    Task<IHttpActionResult> postTask = containersController.Create();
                    postTask.Wait();
                    Task<HttpResponseMessage> resultTask = postTask.Result.ExecuteAsync(new CancellationToken());
                    resultTask.Wait();
                    resultTask.Result.IsSuccessStatusCode.should_be_false();
                };
            };

            context["when  properties are not passed to the endpoint"] = () =>
            {
                before = () =>
                {
                    string containerHandle = null;
                    containerHandle = Guid.NewGuid().ToString();

                    mockCreateContainerService.Setup(x => x.CreateContainer(It.IsAny<String>()))
                        .Returns((String x) => Task.FromResult(x));
                    containersController.Request.Content =
                        new StringContent("{Handle: \"" + containerHandle + "\"}");
                };

                it["returns a successful status code"] = () =>
                {
                    containersController.Create().Result
                        .ExecuteAsync(new CancellationToken()).Result
                        .IsSuccessStatusCode.should_be_true();
                };
            };
        }
    }
}