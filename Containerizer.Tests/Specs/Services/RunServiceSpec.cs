#region

using System;
using System.Linq;
using Containerizer.Services.Implementations;
using Microsoft.Web.Administration;
using NSpec;
using IronFoundry.Container;
using Moq;
using Containerizer.Controllers;
using Containerizer.Models;

#endregion

namespace Containerizer.Tests.Specs.Services
{
    internal class RunServiceSpec : nspec
    {
        private void describe_OnMessageReceived()
        {
            Mock<IWebSocketEventSender> websocketMock = null;
            Mock<IContainer> containerMock = null;
            Mock<IContainerProcess> processMock = null;
            RunService runService = null;

            before = () =>
            {
                websocketMock = new Mock<IWebSocketEventSender>();
                containerMock = new Mock<IContainer>();
                processMock = new Mock<IContainerProcess>();

                runService = new RunService();
                runService.container = containerMock.Object;

                containerMock.Setup(x => x.Directory).Returns(new Mock<IContainerDirectory>().Object);
                containerMock.Setup(x => x.Run(It.IsAny<ProcessSpec>(), It.IsAny<IProcessIO>())).Returns(processMock.Object);
            };


            context["Process exits with 1"] = () =>
            {
                before = () => processMock.Setup(x => x.WaitForExit()).Returns(1);

                it["sends a close event with data == '1'"] = () =>
                {
                    var apiProcessSpec = new ApiProcessSpec();
                    runService.OnMessageReceived(websocketMock.Object, apiProcessSpec);

                    websocketMock.Verify(x => x.SendEvent("close", "1"));
                };
            };

            context["Process exits with 0"] = () =>
            {
                before = () => processMock.Setup(x => x.WaitForExit()).Returns(0);

                it["sends a close event with data == '0'"] = () =>
                {
                    var apiProcessSpec = new ApiProcessSpec();
                    runService.OnMessageReceived(websocketMock.Object, apiProcessSpec);

                    websocketMock.Verify(x => x.SendEvent("close", "0"));
                };
            };
        }


        /* FIXME
        private void describe_()
        {
            context["#AddPort"] = () =>
            {
                IContainerCreationService containerService = null;
                string containerId = null;

                before = () =>
                {
                    containerService = new ContainerService(new ContainerServiceFactory());
                    containerId =
                        containerService.CreateContainer(Guid.NewGuid().ToString());
                    containerId.should_not_be_null();
                };

                after = () =>
                {
                    Helpers.RemoveExistingSite(containerId, containerId);
                };

                it["Adds a binding to the container in IIS"] = () =>
                {
                    new NetInService().AddPort(7868, containerId);
                    var serverManager = ServerManager.OpenRemote("localhost");
                    var existingSite = serverManager.Sites.First(x => x.Name == containerId);
                    existingSite.Bindings.Any(x => x.EndPoint.Port == 7868).should_be_true();
                };

                context["when there is a binding on port 0"] = () =>
                {
                    it["removes the port 0 binding"] = () =>
                    {
                        var existingSite = ServerManager.OpenRemote("localhost").Sites.First(x => x.Name == containerId);
                        existingSite.Bindings.Any(x => x.EndPoint.Port == 0).should_be_true();

                        new NetInService().AddPort(7868, containerId);

                        existingSite = ServerManager.OpenRemote("localhost").Sites.First(x => x.Name == containerId);
                        existingSite.Bindings.Any(x => x.EndPoint.Port == 0).should_be_false();
                    };
                };

                context["when port is zero"] = () =>
                {
                    it["picks an unused port and returns it"] = () =>
                    {
                        var port = new NetInService().AddPort(0, containerId);
                        port.should_not_be(0);
                    };
                };
            };
        }
        */
    }
}