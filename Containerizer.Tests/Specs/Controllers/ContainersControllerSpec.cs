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
            Mock<IContainerPathService> mockContainerPathService = null;
            Mock<IContainerService> mockContainerService = null;
            Mock<IPropertyService> mockPropertyService = null;

            before = () =>
            {
                mockContainerPathService = new Mock<IContainerPathService>();
                mockContainerService = new Mock<IContainerService>();
                mockPropertyService = new Mock<IPropertyService>();
                containersController = new ContainersController(mockContainerPathService.Object,
                    mockContainerService.Object, mockPropertyService.Object)
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
                string containerUserPath = null;
                ContainerSpecApiModel specModel = null;
                CreateResponse result = null;

                before = () =>
                {
                    containerHandle = Guid.NewGuid().ToString();
                    containerUserPath = Path.Combine(@"C:\containerizer", containerHandle, @"user");

                    var mockContainerDirectory = new Mock<IContainerDirectory>();
                    mockContainerDirectory.Setup(x => x.MapUserPath(It.IsAny<string>()))
                        .Returns((string path) => Path.Combine(containerUserPath, path));

                    var mockContainer = new Mock<IContainer>();
                    mockContainer.Setup(x => x.Handle).Returns(containerHandle);
                    mockContainer.Setup(x => x.Directory).Returns(mockContainerDirectory.Object);
                    
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
                        result.Id.should_be(containerHandle);
                    };

                    it["sets properties"] = () =>
                    {
                        mockPropertyService.Verify(
                            x =>
                                x.BulkSetWithContainerPath(containerUserPath, It.Is((Dictionary<string, string> y) => y[key] == value)));
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
                        result.Id.should_be(containerHandle);
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