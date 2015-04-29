#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Results;
using System.Xml.Schema;
using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using Moq;
using Newtonsoft.Json;
using NSpec;
using IronFrame;
using Containerizer.Models;
using System.IO;
using Logger;
using System.DirectoryServices.AccountManagement;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class ContainerProcessControllerSpec : nspec
    {

        Mock<IContainer> mockContainerWithHandle(string handle)
        {
            var container = new Mock<IContainer>();
            container.Setup(x => x.Handle).Returns(handle);
            return container;
        }

        private void describe_()
        {
            ContainerProcessController containerProcessController = null;
            Mock<IContainerService> mockContainerService = null;
            int pid = 9876;

            before = () =>
            {
                mockContainerService = new Mock<IContainerService>();
                containerProcessController = new ContainerProcessController(mockContainerService.Object)
                {
                    Configuration = new HttpConfiguration(),
                    Request = new HttpRequestMessage()
                };
            };


            describe["Stop"] = () =>
            {
                IHttpActionResult result = null;
                act = () => result = containerProcessController.Stop("handle", pid);

                context["container exists"] = () =>
                {
                    Mock<IContainer> mockContainer = null;
                    before = () =>
                    {
                        mockContainer = mockContainerWithHandle("handle");
                        mockContainerService.Setup(x => x.GetContainerByHandle("handle"))
                            .Returns(mockContainer.Object);
                    };

                    context["process exists"] = () =>
                    {
                        Mock<IContainerProcess> mockProcess = null;
                        before = () =>
                        {
                            mockProcess = new Mock<IContainerProcess>();
                            mockContainer.Setup(x => x.FindProcessById(pid)).Returns(mockProcess.Object);
                        };

                        it["returns OK"] = () => result.should_cast_to<OkResult>();
                        it["calls Kill on process"] = () => mockProcess.Verify(x => x.Kill());
                    };

                    context["process does not exist"] = () =>
                    {
                        before = () => mockContainer.Setup(x => x.FindProcessById(pid)).Returns(null as IContainerProcess);

                        it["returns NotFound"] = () => result.should_cast_to<NotFoundResult>();
                    };
                };

                context["container does not exist"] = () =>
                {
                    before = () => mockContainerService.Setup(x => x.GetContainerByHandle("handle")).Returns(null as IContainer);

                    it["returns NotFound"] = () => result.should_cast_to<NotFoundResult>();
                };
            };
        }
    }
}