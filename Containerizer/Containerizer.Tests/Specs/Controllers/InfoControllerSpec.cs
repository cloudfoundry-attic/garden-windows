using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NSpec;
using Containerizer.Controllers;
using System.Web.Http;
using Containerizer.Models;
using System.Web.Http.Results;
using IronFrame;
using Containerizer.Services.Interfaces;
using Containerizer.Services.Implementations;

namespace Containerizer.Tests.Specs.Controllers
{
    class InfoControllerSpec : nspec
    {
        private void describe_()
        {
            describe[Controller.Index] = () =>
            {

                Mock<IContainerInfoService> mockContainerService = null;
                string handle = "container-handle";
                InfoController controller = null;
                IHttpActionResult result = null;

                before = () =>
                {
                    mockContainerService = new Mock<IContainerInfoService>();
                    controller = new InfoController(mockContainerService.Object);
                };

                act = () => result = controller.GetInfo(handle);


                context["when the container exists"] = () =>
                {
                    ContainerInfoApiModel info = null;

                    before = () =>
                    {
                        info = new ContainerInfoApiModel();
                        mockContainerService.Setup(x => x.GetInfoByHandle(handle))
                            .Returns(info);
                    };

                    it["returns info about the container"] = () =>
                    {
                        var jsonResult = result.should_cast_to<JsonResult<ContainerInfoApiModel>>();
                        jsonResult.Content.should_be(info);
                    };
                };

                context["when the container does not exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.GetInfoByHandle(handle))
                            .Returns((ContainerInfoApiModel)null);
                    };

                    it["returns not found"] = () =>
                    {
                        result.should_cast_to<NotFoundResult>();
                    };
                };
            };
        }
    }
}