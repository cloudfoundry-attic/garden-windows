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
    internal class GraceTimeControllerSpec : nspec
    {
        private void describe_()
        {
            Mock<IContainerService> mockContainerService = null;
            GraceTimeController GraceController = null;
            string handle = null;

            Mock<IContainer> mockContainer = null;

            before = () =>
            {
                mockContainerService = new Mock<IContainerService>();
                GraceController = new GraceTimeController(mockContainerService.Object);

                handle = Guid.NewGuid().ToString();

                mockContainer = new Mock<IContainer>();
                mockContainerService.Setup(x => x.GetContainerByHandle(handle)).Returns(mockContainer.Object);

            };

            describe["#SetGraceTime"] = () =>
            {
                const long graceTimeInNanoSeconds = 1000 * 1000 * 10;
                GraceTime grace = null;
                IHttpActionResult result = null;

                before = () =>
                {
                    grace = new GraceTime { GraceTimeInNanoSeconds = graceTimeInNanoSeconds };
                };
                act = () =>
                {
                    result = GraceController.SetGraceTime(handle, grace);
                };

                it["sets the grace time on the container"] = () =>
                {
                    mockContainer.Verify(x => x.SetProperty("GraceTime", graceTimeInNanoSeconds.ToString()));
                };

                context["when grace time is null"] = () =>
                {
                    before = () =>
                    {
                        grace = new GraceTime { };
                    };

                    it["sets the grace time on the container to null"] = () =>
                    {
                        mockContainer.Verify(x => x.SetProperty("GraceTime", null));
                    };
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
            
            describe["#GetGraceTime"] = () =>
            {
                it["returns the current grace time of the container"] = () =>
                {
                    mockContainer.Setup(x => x.GetProperty("GraceTime")).Returns("1234");

                    var result = GraceController.GetGraceTime(handle);
                    var jsonResult = result.should_cast_to<JsonResult<GraceTime>>();
                    jsonResult.Content.GraceTimeInNanoSeconds.should_be(1234);
                };

                context["when grace time is null"] = () =>
                {
                    before = () =>
                    {
                        mockContainer.Setup(x => x.GetProperty("GraceTime")).Returns<System.Func<string>>(null);
                    };

                    it["sets the grace time on the container to null"] = () =>
                    {
                        var result = GraceController.GetGraceTime(handle);
                        var jsonResult = result.should_cast_to<JsonResult<GraceTime>>();
                        jsonResult.Content.GraceTimeInNanoSeconds.should_be(null);
                    };
                };

                context["when the container does not exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.GetContainerByHandle(It.IsAny<string>())).Returns(null as IContainer);
                    };

                    it["Returns not found"] = () =>
                    {
                        var result = GraceController.GetGraceTime(handle);
                        result.should_cast_to<NotFoundResult>();
                    };
                };
            };
        }
    }
}