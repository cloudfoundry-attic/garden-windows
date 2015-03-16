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
                    runService.Run(websocketMock.Object, apiProcessSpec);

                    websocketMock.Verify(x => x.SendEvent("close", "1"));
                };
            };

            context["Process exits with 0"] = () =>
            {
                before = () => processMock.Setup(x => x.WaitForExit()).Returns(0);

                it["sends a close event with data == '0'"] = () =>
                {
                    var apiProcessSpec = new ApiProcessSpec();
                    runService.Run(websocketMock.Object, apiProcessSpec);
                    
                    websocketMock.Verify(x => x.SendEvent("close", "0"));
                };
            };
        }

    }
}