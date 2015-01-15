#region

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Xml.Schema;
using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using Moq;
using Newtonsoft.Json;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class ContainersControllerSpec : nspec
    {
        private void describe_()
        {
            ContainersController containersController = null;
            Mock<IContainerPathService> mockContainerPathService = null;
            Mock<IContainerService> mockCreateContainerService = null;
            Mock<IPropertyService> mockPropertyService = null;

            before = () =>
            {
                mockContainerPathService = new Mock<IContainerPathService>();
                mockCreateContainerService = new Mock<IContainerService>();
                mockPropertyService = new Mock<IPropertyService>();
                containersController = new ContainersController(mockContainerPathService.Object,
                    mockCreateContainerService.Object, mockPropertyService.Object)
                {
                    Configuration = new HttpConfiguration(),
                    Request = new HttpRequestMessage()
                };
            };

            describe[Controller.Index] = () =>
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
                    result.VerifiesSuccessfulStatusCode();
                };

                it["returns a list of container ids as strings"] = () =>
                {
                    var jsonString = result.Content.ReadAsString(); // Json();
                    var json = JsonConvert.DeserializeObject<string[]>(jsonString);
                    json.should_contain("MyFirstContainer");
                    json.should_contain("MySecondContainer");
                };
            };

            describe[Controller.Update] = () =>
            {
                context["when the container is created successfully"] = () =>
                {
                    string containerHandle = null;
                    Dictionary<string, string> properties = null;
                    string key = null;
                    string value = null;
                    IHttpActionResult result = null;

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
                            .Returns((String x) => x);
                        containersController.Request.Content =
                            new StringContent("{Handle: \"" + containerHandle + "\", Properties:{\"" + key + "\": \"" +
                                              value + "\"}}");
                        result = containersController.Create().Result;
                    };

                    it["returns a successful status code"] = () =>
                    {
                        result.VerifiesSuccessfulStatusCode();
                    };

                    it["returns the passed in container's id"] = () =>
                    {
                        result.ReadContentAsJson()["id"].ToString().should_be(containerHandle);
                    };

                    it["sets properties"] = () =>
                    {
                        mockPropertyService.Verify(
                            x =>
                                x.BulkSet(containerHandle, It.Is((Dictionary<string, string> y) => y[key] == value)));
                    };
                };

                context["when properties are not passed to the endpoint"] = () =>
                {
                    IHttpActionResult result = null;

                    before = () =>
                    {
                        string containerHandle = null;
                        containerHandle = Guid.NewGuid().ToString();

                        mockCreateContainerService.Setup(x => x.CreateContainer(It.IsAny<String>()))
                            .Returns((String x) => x);
                        containersController.Request.Content =
                            new StringContent("{Handle: \"" + containerHandle + "\"}");

                        result = containersController.Create().Result;
                    };

                    it["returns a successful status code"] = () =>
                    {
                        result.VerifiesSuccessfulStatusCode();
                    };
                };
            };

            describe[Controller.Destroy] = () =>
            {
                HttpResponseMessage result = null;

                act = () => result = containersController.Destroy("MySecondContainer").GetAwaiter().GetResult();

                context["a handle which exists"] = () =>
                {
                    before = () =>
                    {
                        mockContainerPathService.Setup(x => x.ContainerIds())
                            .Returns(new List<string>
                            {
                                "MyFirstContainer",
                                "MySecondContainer",
                                "MyThirdContainer"
                            });
                    };

                    it["returns 200"] = () =>
                    {
                        result.StatusCode.should_be(HttpStatusCode.OK);
                    };

                    it["calls delete on the containerPathService"] = () =>
                    {
                        mockContainerPathService.Verify(x => x.DeleteContainerDirectory("MySecondContainer"));
                    };
                };

                context["a handle which does not exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainerPathService.Setup(x => x.ContainerIds())
                            .Returns(new List<string>
                            {
                                "MyFirstContainer",
                                "MyThirdContainer"
                            });
                    };

                    it["returns 404"] = () =>
                    {
                        result.StatusCode.should_be(HttpStatusCode.NotFound);
                    };

                };
            };
        }
    }
}