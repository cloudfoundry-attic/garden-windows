#region

using System;
using System.Collections.Generic;
using System.Web.Http;
using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using Moq;
using NSpec;
using IronFoundry.Container;
using System.Web.Http.Results;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class LimitsControllerSpec : nspec
    {
        private void describe_()
        {
            Mock<IContainerService> mockContainerService = null;
            LimitsController LimitsController = null;

            before = () =>
            {
                mockContainerService = new Mock<IContainerService>();
                LimitsController = new LimitsController(mockContainerService.Object);
            };

            describe["LimitMemory"] = () =>
            {
                string containerId = null;
                ulong limitInBytes = 876;
                MemoryLimits limits = null;
                IHttpActionResult result = null;
                Mock<IContainer> mockContainer = null;

                before = () =>
                {
                    containerId = Guid.NewGuid().ToString();
                    limits = new MemoryLimits { LimitInBytes = limitInBytes };
                    mockContainer = new Mock<IContainer>();

                    mockContainerService.Setup(x => x.GetContainerByHandle(containerId)).Returns(mockContainer.Object);
                };

                act = () =>
                {
                    result = LimitsController.LimitMemory(containerId, limits);
                };

                it["sets limits on the container"] = () =>
                {
                    mockContainer.Verify(x => x.LimitMemory(limitInBytes));
                };

                context["when the container does not exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.GetContainerByHandle(It.IsAny<string>())).Returns(null as IContainer);
                    };

                    it["Returns not found"] = () =>
                    {
                        result.should_cast_to<NotFoundResult>();
                    };
                };
            };
        }
    }
}