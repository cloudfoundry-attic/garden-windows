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
    class BulkMetricsControllerSpec : nspec
    {
        private void describe_()
        {
            describe["#BulkMetrics"] = () =>
            {
                List<ContainerMetricsApiModel> metrics = null;

                Mock<IContainerInfoService> mockContainerService = null;
                var handles = new string[] { "handle1", "handle2" };
                BulkMetricsController controller = null;
                Dictionary<string, BulkMetricsResponse> result = null;

                before = () =>
                {
                    metrics = new List<ContainerMetricsApiModel> { new ContainerMetricsApiModel(), new ContainerMetricsApiModel() };
                    mockContainerService = new Mock<IContainerInfoService>();
                    controller = new BulkMetricsController(mockContainerService.Object);
                };

                act = () => result = controller.BulkMetrics(handles);

                context["when all requested containers exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.GetMetricsByHandle(handles[0])).Returns(metrics[0]);
                        mockContainerService.Setup(x => x.GetMetricsByHandle(handles[1])).Returns(metrics[1]);
                    };

                    it["returns info about the container"] = () =>
                    {
                        result.Count.should_be(2);
                        result[handles[0]].Metrics.should_be(metrics[0]);
                        result[handles[1]].Metrics.should_be(metrics[1]);
                    };
                };

                context["when the container does not exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.GetMetricsByHandle(handles[0])).Returns(null as ContainerMetricsApiModel);
                        mockContainerService.Setup(x => x.GetMetricsByHandle(handles[1])).Returns(metrics[1]);
                    };

                    it["is not returned"] = () =>
                    {
                        result.Count.should_be(1);
                        result[handles[1]].Metrics.should_be(metrics[1]);
                    };
                };
            };
        }
    }
}