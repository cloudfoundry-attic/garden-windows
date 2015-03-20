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

            before = () =>
            {
                mockContainerService = new Mock<IContainerService>();
                mockPropertyService = new Mock<IContainerPropertyService>();
                containersController = new ContainersController(mockContainerService.Object, mockPropertyService.Object)
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
                    mockContainerService.Setup(x => x.GetContainers())
                        .Returns(new List<IContainer>
                        {
                            mockContainerWithHandle("MyFirstContainer"),
                            mockContainerWithHandle("MySecondContainer")
                        });
                    result = containersController.Index();
                };

                it["returns a list of container ids as strings"] = () =>
                {
                    result.should_contain("MyFirstContainer");
                    result.should_contain("MySecondContainer");
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