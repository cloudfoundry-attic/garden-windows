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
using IronFoundry.Container;
using Containerizer.Models;
using System.IO;
using Logger;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class ContainersControllerSpec : nspec
    {

        IContainer mockContainerWithHandle(string handle)
        {
            var container = new Mock<IContainer>();
            container.Setup(x => x.Handle).Returns(handle);
            return container.Object;
        }

        private void describe_()
        {
            ContainersController containersController = null;
            Mock<IContainerService> mockContainerService = null;
            Mock<IContainerPropertyService> mockPropertyService = null;
            Mock<ILogger> mockLogger = null;


            before = () =>
            {
                mockContainerService = new Mock<IContainerService>();
                mockPropertyService = new Mock<IContainerPropertyService>();
                mockLogger = new Mock<ILogger>();
                containersController = new ContainersController(mockContainerService.Object, mockPropertyService.Object, mockLogger.Object)
                {
                    Configuration = new HttpConfiguration(),
                    Request = new HttpRequestMessage()
                };
            };


            describe[Controller.Index] = () =>
            {
                IReadOnlyList<string> result = null;

                before = () =>
                {
                    var container1 = mockContainerWithHandle("handle1");
                    var container2 = mockContainerWithHandle("handle2");
                    var container3 = mockContainerWithHandle("handle3");

                    mockContainerService.Setup(x => x.GetContainers())
                        .Returns(new List<IContainer>
                        {
                            container1, container2, container3
                        });

                    mockPropertyService.Setup(x => x.GetProperties(container1)).Returns(new Dictionary<string, string> { { "a", "b" } });
                    mockPropertyService.Setup(x => x.GetProperties(container2)).Returns(new Dictionary<string, string> { { "a", "b" }, { "c", "d" }, { "e", "f" } });
                    mockPropertyService.Setup(x => x.GetProperties(container3)).Returns(new Dictionary<string, string> { { "e", "f" } });
                };

                it["when filter is provided returns a filtered list of container id's as strings"] = () =>
                {
                    result = containersController.Index("{\"a\":\"b\", \"e\":\"f\"}");

                    result.should_not_be_null();
                    result.Count.should_be(1);
                    result.should_contain("handle2");
                };

                it["without filter returns a list of all containers id's as strings"] = () =>
                {
                    result = containersController.Index();

                    result.should_not_be_null();
                    result.Count.should_be(3);
                    result.should_contain("handle1");
                    result.should_contain("handle2");
                    result.should_contain("handle3");
                };
            };

            describe["#Create"] = () =>
            {

                string containerHandle = null;
                string containerPath = null;
                ContainerSpecApiModel specModel = null;
                CreateResponse result = null;
                Mock<IContainer> mockContainer = null;

                before = () =>
                {
                    containerHandle = Guid.NewGuid().ToString();
                    containerPath = Path.Combine(@"C:\containerizer", containerHandle);

                    mockContainer = new Mock<IContainer>();
                    mockContainer.Setup(x => x.Handle).Returns(containerHandle);
                    
                    mockContainerService.Setup(x => x.CreateContainer(It.IsAny<ContainerSpec>()))
                        .Returns(mockContainer.Object);
                };

                act = () => result = containersController.Create(specModel);

                context["when the container is created successfully"] = () =>
                {
                    string key = null;
                    string value = null;
                    before = () =>
                    {
                        key = "hiwillyou";
                        value = "bemyfriend";

                        specModel = new ContainerSpecApiModel
                        {
                            Handle = containerHandle,
                            Properties = new Dictionary<string, string>
                            {
                                {key, value}
                            },
                        };
                    };

                    it["returns the passed in container's id"] = () =>
                    {
                        result.Handle.should_be(containerHandle);
                    };

                    it["sets properties"] = () =>
                    {
                        mockPropertyService.Verify(
                            x =>
                                x.SetProperties(mockContainer.Object, It.Is((Dictionary<string, string> y) => y[key] == value)));
                    };
                };

                context["when properties are not passed to the endpoint"] = () =>
                {
                    before = () =>
                    {
                        specModel = new ContainerSpecApiModel
                        {
                            Handle = containerHandle,
                            Properties = null,
                        };
                    };

                    it["returns the passed in container's id"] = () =>
                    {
                        result.Handle.should_be(containerHandle);
                    };
                };
            };

            describe["#Stop"] = () =>
            {
                IHttpActionResult result = null;

                act = () => result = containersController.Stop("MySecondContainer");

                context["a handle which exists"] = () =>
                {
                    Mock<IContainer> mockContainer = null;
                    before = () =>
                    {
                        mockContainer = new Mock<IContainer>();
                        mockContainerService.Setup(x => x.GetContainerByHandle("MySecondContainer")).Returns(mockContainer.Object);
                    };

                    it["returns 200"] = () =>
                    {
                        result.should_cast_to<System.Web.Http.Results.OkResult>();
                    };

                    it["calls delete on the containerPathService"] = () =>
                    {
                        mockContainer.Verify(x => x.Stop(true));
                    };
                };

                context["a handle which does not exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.GetContainerByHandle("MySecondContainer")).Returns(null as IContainer);
                    };

                    it["returns 404"] = () =>
                    {
                        result.should_cast_to<System.Web.Http.Results.NotFoundResult>();
                    };
                };
            };

            describe["#Destroy"] = () =>
            {
                IHttpActionResult result = null;

                act = () => result = containersController.Destroy("MySecondContainer");

                context["a handle which exists"] = () =>
                {
                    Mock<IContainer> mockContainer = null;
                    before = () =>
                    {
                        mockContainer = new Mock<IContainer>();
                        mockContainerService.Setup(x => x.GetContainerByHandle("MySecondContainer")).Returns(mockContainer.Object);
                    };

                    it["returns 200"] = () =>
                    {
                        result.should_cast_to<System.Web.Http.Results.OkResult>();
                    };

                    it["calls delete on the containerPathService"] = () =>
                    {
                        mockContainerService.Verify(x => x.DestroyContainer("MySecondContainer"));
                    };
                };

                context["a handle which does not exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.GetContainerByHandle("MySecondContainer")).Returns(null as IContainer);
                    };

                    it["returns 404"] = () =>
                    {
                        result.should_cast_to<System.Web.Http.Results.NotFoundResult>();
                    };

                };
            };
        }
    }
}