#region

using System;
using System.Web.Http;
using System.Web.Http.Results;
using Containerizer.Controllers;
using IronFrame;
using Moq;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class NetControllerSpec : nspec
    {
        private void describe_()
        {
            Mock<IContainerService> mockContainerService = null;
            NetController netController = null;

            before = () =>
            {
                mockContainerService = new Mock<IContainerService>();

                netController = new NetController(mockContainerService.Object);
            };

            describe[Controller.Create] = () =>
            {
                string containerId = null;
                var requestedHostPort = 0;
                var requestedContainerPort = 5678;
                IHttpActionResult result = null;
                Mock<IContainer> mockContainer = null;

                before = () =>
                {
                    containerId = Guid.NewGuid().ToString();
                    requestedHostPort = 0;
                    mockContainer = new Mock<IContainer>();

                    mockContainerService.Setup(x => x.GetContainerByHandle(containerId)).Returns(mockContainer.Object);
                };

                act =
                    () =>
                    {
                        result = netController.Create(containerId,
                            new NetInRequest {ContainerPort = requestedContainerPort, HostPort = requestedHostPort});
                    };

                it["reserves the Port in the container"] =
                    () => { mockContainer.Verify(x => x.ReservePort(requestedHostPort)); };

                context["when the container does not exist"] = () =>
                {
                    before =
                        () =>
                        {
                            mockContainerService.Setup(x => x.GetContainerByHandle(It.IsAny<string>()))
                                .Returns(null as IContainer);
                        };

                    it["Returns not found"] = () => { result.should_cast_to<NotFoundResult>(); };
                };

                context["reserving the Port in the container succeeds and returns a Port"] = () =>
                {
                    const int returnedPort = 8765;
                    before = () => { mockContainer.Setup(x => x.ReservePort(requestedHostPort)).Returns(returnedPort); };

                    it["calls reservePort on the container"] =
                        () => { mockContainer.Verify(x => x.ReservePort(requestedHostPort)); };

                    context["container reservePort succeeds and returns a Port"] = () =>
                    {
                        before = () => { mockContainer.Setup(x => x.ReservePort(requestedHostPort)).Returns(returnedPort); };

                        it["returns the Port that the net in service returns"] = () =>
                        {
                            var jsonResult = result.should_cast_to<JsonResult<NetInResponse>>();
                            jsonResult.Content.HostPort.should_be(8765);
                        };


                        it["sets the containerport property key lookup"] =
                        () =>
                        {
                            mockContainer.Verify(
                                x => x.SetProperty("ContainerPort:" + requestedContainerPort.ToString(), returnedPort.ToString()));
                        };
                    };

                    context["reserving the Port in the container fails and throws an exception"] = () =>
                    {
                        before =
                            () =>
                            {
                                mockContainer.Setup(x => x.ReservePort(requestedHostPort)).Throws(new Exception("BOOM"));
                            };

                        it["returns an error"] = () =>
                        {
                            var errorResult = result.should_cast_to<ExceptionResult>();
                            errorResult.Exception.Message.should_be("BOOM");
                        };
                    };
                };
            };
        }
    }
}