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

namespace Containerizer.Tests.Specs.Controllers
{
    class BulkInfoControllerSpec : nspec
    {
        private void describe_()
        {
            describe["#BulkInfo"] = () =>
            {
                List<ContainerInfoApiModel> info = null;

                Mock<IContainerInfoService> mockContainerService = null;
                var handles = new string[] { "handle1", "handle2" };
                BulkInfoController controller = null;
                Dictionary<string, BulkInfoResponse> result = null;

                before = () =>
                {
                    info = new List<ContainerInfoApiModel> { new ContainerInfoApiModel(), new ContainerInfoApiModel() };
                    mockContainerService = new Mock<IContainerInfoService>();
                    controller = new BulkInfoController(mockContainerService.Object);
                };

                act = () => result = controller.BulkInfo(handles);


                context["when all requested containers exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.GetInfoByHandle(handles[0])).Returns(info[0]);
                        mockContainerService.Setup(x => x.GetInfoByHandle(handles[1])).Returns(info[1]);
                    };

                    it["returns info about the container"] = () =>
                    {
                        result.Count.should_be(2);
                        result[handles[0]].Info.should_be(info[0]);

                        result[handles[1]].Info.should_be(info[1]);
                    };
                };

                context["when the container does not exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.GetInfoByHandle(handles[0]))
                            .Returns((ContainerInfoApiModel)null);
                        mockContainerService.Setup(x => x.GetInfoByHandle(handles[1]))
                            .Returns((ContainerInfoApiModel)info[1]);
                    };

                    it["is not returned"] = () =>
                    {
                        result.Count.should_be(1);
                        result[handles[1]].Info.should_be(info[1]);
                    };
                };
            };
        }
    }
}