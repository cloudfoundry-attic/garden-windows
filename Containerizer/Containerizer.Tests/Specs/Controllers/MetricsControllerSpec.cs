#region

using Containerizer.Controllers;
using Containerizer.Models;
using Containerizer.Services.Interfaces;
using Moq;
using NSpec;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class MetricsControllerSpec : nspec
    {
        private void describe_()
        {
            Mock<IContainerInfoService> mockContainerInfoService = null;
            MetricsController metricsController = null;
            string containerHandle = null;
            const ulong privateBytes = 28;
            ContainerMetricsApiModel containerMetrics = null;
            before = () =>
            {
                mockContainerInfoService = new Mock<IContainerInfoService>();
                metricsController = new MetricsController(mockContainerInfoService.Object)
                {
                    Configuration = new HttpConfiguration(),
                    Request = new HttpRequestMessage()
                };
                containerHandle = Guid.NewGuid().ToString();
                containerMetrics = new ContainerMetricsApiModel
                {
                    MemoryStat = new ContainerMemoryStatApiModel
                    {
                        TotalBytesUsed = privateBytes
                    }
                };

                mockContainerInfoService.Setup(x => x.GetMetricsByHandle(containerHandle))
                    .Returns(() => containerMetrics);
            };

            describe[Controller.Show] = () =>
            {
                IHttpActionResult result = null;

                act = () => result = metricsController.Show(containerHandle);

                it["returns a successful status code"] = () =>
                {
                    result.VerifiesSuccessfulStatusCode();
                };

                it["returns the container metrics as a json"] = () =>
                {
                    var message = result.should_cast_to<JsonResult<ContainerMetricsApiModel>>();
                    message.Content.should_be(containerMetrics);
                };

                context["when the container does not exist"] = () =>
                {
                    before = () => containerMetrics = null;

                    it["returns a 404"] = () =>
                    {
                        var message = result.should_cast_to<ResponseMessageResult>();
                        message.Response.StatusCode.should_be(HttpStatusCode.NotFound);
                    };
                };
            };

        }
    }
}