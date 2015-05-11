using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NLog.LayoutRenderers;
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
    class MetricsControllerSpec : nspec
    {
        private void describe_()
        {
            describe[Controller.Index] = () =>
            {

                Mock<IContainerService> mockContainerService = null;
                string handle = "container-handle";
                MetricsController controller = null;
                IHttpActionResult result = null;

                before = () =>
                {
                    mockContainerService = new Mock<IContainerService>();
                    controller = new MetricsController(mockContainerService.Object);
                };

                act = () => result = controller.Get(handle);


                context["when the container exists"] = () =>
                {
                    Mock<IContainer> container = null;

                    before = () =>
                    {
                        container = new Mock<IContainer>();
                        container.Setup(x => x.GetInfo()).Returns(new ContainerInfo
                        {
                            CpuStat = new ContainerCpuStat { TotalProcessorTime = new TimeSpan(0, 0, 0, 1, 2) },
                            MemoryStat = new ContainerMemoryStat { PrivateBytes = 5678 },
                        });
                        mockContainerService.Setup(x => x.GetContainerByHandle(handle)).Returns(container.Object);
                    };

                    it["returns metrics about the container"] = () =>
                    {
                        result.should_cast_to<JsonResult<Metrics>>();
                    };

                    it["returns cpu metrics"] = () =>
                    {
                        var metrics = result.should_cast_to<JsonResult<Metrics>>().Content;
                        metrics.CPUStat.Usage.should_be(1002);
                    };

                    it["returns memory metrics"] = () =>
                    {
                        var metrics = result.should_cast_to<JsonResult<Metrics>>().Content;
                        metrics.MemoryStat.TotalBytesUsed.should_be(5678);
                    };

                    xit["returns disk metrics"] = () =>
                    {
                        var metrics = result.should_cast_to<JsonResult<Metrics>>().Content;
                        metrics.DiskStat.BytesUsed.should_be(2345); // FIXME
                    };
                };

                context["when the container does not exist"] = () =>
                {
                    before = () => mockContainerService.Setup(x => x.GetContainerByHandle(handle)).Returns(null as IContainer);

                    it["returns not found"] = () =>
                    {
                        result.should_cast_to<NotFoundResult>();
                    };
                };
            };
        }
    }
}