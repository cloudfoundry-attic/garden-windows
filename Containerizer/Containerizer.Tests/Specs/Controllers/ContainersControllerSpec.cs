#region

using System.Web.Http.Results;
using Containerizer.Controllers;
using Containerizer.Models;
using IronFrame;
using Logger;
using Moq;
using NSpec;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web.Http;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class ContainersControllerSpec : nspec
    {

        Mock<IContainer> mockContainerWithHandle(string handle)
        {
            var container = new Mock<IContainer>();
            container.Setup(x => x.Handle).Returns(handle);
            return container;
        }

        private void describe_()
        {
            ContainersController containersController = null;
            Mock<IContainerService> mockContainerService = null;
            Mock<ILogger> mockLogger = null;


            before = () =>
            {
                mockContainerService = new Mock<IContainerService>();
                mockLogger = new Mock<ILogger>();
                containersController = new ContainersController(mockContainerService.Object, mockLogger.Object)
                {
                    Configuration = new HttpConfiguration(),
                    Request = new HttpRequestMessage()
                };
            };


            describe[Controller.Index] = () =>
            {
                IReadOnlyList<string> result = null;
                Mock<IContainer> mockContainer1 = null;

                before = () =>
                {
                    mockContainer1 = mockContainerWithHandle("handle1");
                    var mockContainer2 = mockContainerWithHandle("handle2");
                    var mockContainer3 = mockContainerWithHandle("handle3");

                    mockContainerService.Setup(x => x.GetContainers())
                        .Returns(new List<IContainer>
                        {
                            mockContainer1.Object,
                            mockContainer2.Object,
                            mockContainer3.Object
                        });

                    mockContainer1.Setup(x => x.GetProperties()).Returns(new Dictionary<string, string> { { "a", "b" } });
                    mockContainer2.Setup(x => x.GetProperties()).Returns(new Dictionary<string, string> { { "a", "b" }, { "c", "d" }, { "e", "f" } });
                    mockContainer3.Setup(x => x.GetProperties()).Returns(new Dictionary<string, string> { { "e", "f" } });
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

                it["filters out destroyed containers when a query is passed"] = () =>
                {
                    mockContainer1.Setup(x => x.GetProperties())
                        .Returns(() => { throw new InvalidOperationException(); });

                    result = containersController.Index("{}");
                    result.should_not_contain("handle1");
                    result.should_contain("handle2");
                    result.should_contain("handle3");
                };
            };

            describe["#Create"] = () =>
            {
                Mock<IContainer> mockContainer = null;
                ContainerSpecApiModel containerSpec = null;
                before = () =>
                {
                    mockContainer = mockContainerWithHandle("thisHandle");
                    containerSpec = new ContainerSpecApiModel
                    {
                        Limits = new Limits
                        {
                            MemoryLimits = new MemoryLimits { LimitInBytes = 500 },
                            CpuLimits = new CpuLimits { Weight = 9999 },
                            DiskLimits = new DiskLimits { ByteHard = 999 }
                        },
                        GraceTime = 1234
                    };
                };

                context["on success"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.CreateContainer(It.IsAny<ContainerSpec>()))
                            .Returns(mockContainer.Object);
                    };

                    it["returns a handle"] = () =>
                    {
                        var result = containersController.Create(containerSpec);
                        result.Handle.should_be("thisHandle");
                    };

                    it["sets ActiveProcessLimit on the Container"] = () =>
                    {
                        containersController.Create(containerSpec);
                        mockContainer.Verify(x => x.SetActiveProcessLimit(10));
                    };

                    it["sets PriorityClass on the Container"] = () =>
                    {
                        containersController.Create(containerSpec);
                        mockContainer.Verify(x => x.SetPriorityClass(ProcessPriorityClass.BelowNormal));
                    };

                    it["sets limits on the container"] = () =>
                    {
                        containersController.Create(containerSpec);
                        mockContainer.Verify(x => x.LimitMemory(500));
                        mockContainer.Verify(x => x.LimitDisk(999));
                    };

                    it["sets GraceTime on the Container"] = () =>
                    {
                        containersController.Create(containerSpec);
                        mockContainer.Verify(x => x.SetProperty("GraceTime", "1234"));
                    };

                    it["doesn't set limits when the values are null"] = () =>
                    {

                        containerSpec.Limits.MemoryLimits.LimitInBytes = null;
                        containerSpec.Limits.CpuLimits.Weight = null;
                        containerSpec.Limits.DiskLimits.ByteHard = null;

                        containersController.Create(containerSpec);

                        mockContainer.Verify(x => x.LimitMemory(It.IsAny<ulong>()), Times.Never());
                        mockContainer.Verify(x => x.LimitDisk(It.IsAny<ulong>()), Times.Never());
                    };

                    it["hard-codes the CPU limits to 5"] = () =>
                    {
                        containersController.Create(containerSpec);
                        mockContainer.Verify(x => x.LimitCpu(5));
                    };
                };

                context["on failure"] = () =>
                {
                    before = () => mockContainerService.Setup(x => x.CreateContainer(It.IsAny<ContainerSpec>())).Throws(new Exception());

                    it["throws HttpResponseException"] = () =>
                    {
                        expect<HttpResponseException>(() => containersController.Create(containerSpec));
                    };
                };
            };

            describe["#NetIn"] = () =>
            {
                string containerHandle = null;
                string containerPath = null;
                ContainerSpecApiModel specModel = null;
                CreateResponse result = null;
                Mock<IContainer> mockContainer = null;
                string key = null;
                string value = null;

                context["when the container is created successfully"] = () =>
                {

                    before = () =>
                    {
                        containerHandle = Guid.NewGuid().ToString();
                        containerPath = Path.Combine(@"C:\containerizer", containerHandle);

                        mockContainer = new Mock<IContainer>();
                        mockContainer.Setup(x => x.Handle).Returns(containerHandle);

                        mockContainerService.Setup(x => x.CreateContainer(It.IsAny<ContainerSpec>()))
                            .Returns(mockContainer.Object);

                        key = "hiwillyou";
                        value = "bemyfriend";

                        specModel = new ContainerSpecApiModel
                        {
                            Handle = containerHandle,
                            Properties = new Dictionary<string, string>
                            {
                                {key, value}
                            }
                        };
                    };

                    act = () => result = containersController.Create(specModel);

                    it["returns the passed in container's id"] = () =>
                    {
                        result.Handle.should_be(containerHandle);
                    };

                    it["sets properties"] = () =>
                    {
                        mockContainerService.Verify(
                            x => x.CreateContainer(
                                It.Is<ContainerSpec>(createSpec => createSpec.Properties[key] == value)));
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

                context["when creating a container fails"] = () =>
                {
                    Exception ex = null;

                    act = () =>
                    {
                        try
                        {
                            containersController.Create(new ContainerSpecApiModel()
                            {
                                Handle = "foo"
                            });
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }
                    };

                    context["because the user already exists"] = () =>
                    {

                        before = () =>
                        {
                            mockContainerService.Setup(x => x.CreateContainer(It.IsAny<ContainerSpec>()))
                                .Throws(new PrincipalExistsException());
                        };

                        it["throw HttpResponseException with handle already exists message"] = () =>
                        {
                            var httpResponse = ex.should_cast_to<HttpResponseException>();
                            httpResponse.Response.StatusCode.should_be(HttpStatusCode.Conflict);
                            httpResponse.Response.Content.ReadAsJsonString().should_be("handle already exists: foo");
                        };
                    };

                    context["because other random exception"] = () =>
                    {
                        before = () =>
                        {
                            mockContainerService.Setup(x => x.CreateContainer(It.IsAny<ContainerSpec>())).Throws(new Exception("BOOM"));
                        };

                        it["throw HttpResponseException with the original exception's message"] = () =>
                        {
                            var httpResponse = ex.should_cast_to<HttpResponseException>();
                            httpResponse.Response.StatusCode.should_be(HttpStatusCode.InternalServerError);
                            httpResponse.Response.Content.ReadAsJsonString().should_be("BOOM");
                        };
                    };
                };
            };

            string handle = "MySecondContainer";

            describe["#Stop"] = () =>
            {
                IHttpActionResult result = null;

                act = () => result = containersController.Stop(handle);

                context["a handle which exists"] = () =>
                {
                    Mock<IContainer> mockContainer = null;
                    before = () =>
                    {
                        mockContainer = new Mock<IContainer>();
                        mockContainerService.Setup(x => x.GetContainerByHandle(handle)).Returns(mockContainer.Object);
                    };

                    it["returns 200"] = () =>
                    {
                        result.should_cast_to<System.Web.Http.Results.OkResult>();
                    };

                    it["calls stop on the container"] = () =>
                    {
                        mockContainer.Verify(x => x.Stop(true));
                    };
                };

                context["a handle which does not exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.GetContainerByHandle(handle)).Returns(null as IContainer);
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

                act = () => result = containersController.Destroy(handle);

                context["a handle which exists"] = () =>
                {
                    Mock<IContainer> mockContainer = null;
                    before = () =>
                    {
                        mockContainer = new Mock<IContainer>();
                        mockContainerService.Setup(x => x.GetContainerByHandle(handle)).Returns(mockContainer.Object);
                    };

                    it["returns 200"] = () =>
                    {
                        result.should_cast_to<System.Web.Http.Results.OkResult>();
                    };

                    it["calls delete on the containerPathService"] = () =>
                    {
                        mockContainerService.Verify(x => x.DestroyContainer(handle));
                    };

                    var setupDestroyExceptions = new Action<int>((int errorCount) =>
                    {
                        var callsCount = 0;
                        mockContainerService.Setup(x => x.DestroyContainer(handle)).Callback(() =>
                        {
                            if (callsCount++ < errorCount) throw new IOException("file is in use");
                        });
                    });
                };

                context["a handle which does not exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.GetContainerByHandle(handle)).Returns(null as IContainer);
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