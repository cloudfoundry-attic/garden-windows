#region

using System;
using System.Collections.Generic;
using System.Web.Http;
using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using Moq;
using NSpec;
using IronFrame;
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
            string handle = null;
            ulong limitInBytes = 876;
            MemoryLimits limits = null;
            Mock<IContainer> mockContainer = null;

            before = () =>
            {
                mockContainerService = new Mock<IContainerService>();
                LimitsController = new LimitsController(mockContainerService.Object);

                handle = Guid.NewGuid().ToString();
                limits = new MemoryLimits { LimitInBytes = limitInBytes };
                mockContainer = new Mock<IContainer>();
                mockContainerService.Setup(x => x.GetContainerByHandle(handle)).Returns(mockContainer.Object);
            };

            describe["#LimitMemory"] = () =>
            {
                IHttpActionResult result = null;
                act = () =>
                {
                    result = LimitsController.LimitMemory(handle, limits);
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

            describe["#CurrentMemoryLimit"] = () =>
            {
                it["returns the current limit on the container"] = () =>
                {
                    mockContainer.Setup(x => x.CurrentMemoryLimit()).Returns(3072);
                    var result = LimitsController.CurrentMemoryLimit(handle);
                    var jsonResult = result.should_cast_to<JsonResult<MemoryLimits>>();
                    jsonResult.Content.LimitInBytes.should_be(3072);
                };

                context["when the container does not exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.GetContainerByHandle(It.IsAny<string>())).Returns(null as IContainer);
                    };

                    it["Returns not found"] = () =>
                    {
                        var result = LimitsController.CurrentMemoryLimit(handle);
                        result.should_cast_to<NotFoundResult>();
                    };
                };
            };
        }
    }
}