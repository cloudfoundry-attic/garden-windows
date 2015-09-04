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
using ContainerInfo = Containerizer.Models.ContainerInfo;
using ContainerInfoEntry = Containerizer.Controllers.ContainerInfoEntry;

namespace Containerizer.Tests.Specs.Controllers
{
    class BulkInfoControllerSpec : nspec
    {
        private void describe_()
        {
            describe["#BulkInfo"] = () =>
            {
                List<ContainerInfo> info = null;

                Mock<IContainerInfoService> mockContainerService = null;
                var handles = new string[] { "handle1", "handle2" };
                BulkInfoController controller = null;
                Dictionary<string, ContainerInfoEntry> result = null;

                before = () =>
                {
                    info = new List<ContainerInfo> { new ContainerInfo(), new ContainerInfo() };
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

                context["when GetInfoByHandle throws an exception"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.GetInfoByHandle(handles[0]))
                            .Throws(new Exception("BOOOOM"));
                        mockContainerService.Setup(x => x.GetInfoByHandle(handles[1]))
                            .Returns((ContainerInfo)info[1]);
                    };

                    it["returns each container with an error for the ones that error"] = () =>
                    {
                        result.Count.should_be(2);
                        result[handles[0]].Info.should_be_null();
                        result[handles[0]].Err.ErrorMsg.should_contain("BOOOOM");
                        result[handles[1]].Info.should_be(info[1]);
                        result[handles[1]].Err.should_be_null();
                    };
                };

                context["when the container does not exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.GetInfoByHandle(handles[0]))
                            .Returns((ContainerInfo)null);
                        mockContainerService.Setup(x => x.GetInfoByHandle(handles[1]))
                            .Returns((ContainerInfo)info[1]);
                    };

                    it["returns a not exist error for handle1"] = () =>
                    {
                        result.Count.should_be(2);
                        result[handles[0]].Err.ErrorMsg.should_contain("not exist");
                        result[handles[1]].Info.should_be(info[1]);
                    };
                };
            };
        }
    }
}