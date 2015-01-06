#region

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using Moq;
using Newtonsoft.Json.Linq;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class NetControllerSpec : nspec
    {
        private Mock<INetInService> mockNetInService;
        private NetController netController;

        private void before_each()
        {
            mockNetInService = new Mock<INetInService>();
            netController = new NetController(mockNetInService.Object)
            {
                Configuration = new HttpConfiguration(),
                Request = new HttpRequestMessage()
            };
        }

        private void describe_net_in()
        {
            string containerId = null;
            int requestedContainerPort = 0;
            HttpResponseMessage result = null;

            before = () =>
            {
                containerId = Guid.NewGuid().ToString();
                requestedContainerPort = 5432;
            };

            act = () =>
            {
                netController.Request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("hostPort", requestedContainerPort.ToString())
                });

                result =
                    netController.NetIn(containerId)
                        .GetAwaiter()
                        .GetResult()
                        .ExecuteAsync(new CancellationToken())
                        .GetAwaiter()
                        .GetResult();
            };

            it["returns a successful status code"] = () =>
            {
                result.IsSuccessStatusCode.should_be_true();
            };

            it["calls the net in service with its passed in parameters"] =
                () =>
                {
                    mockNetInService.Verify(x => x.AddPort(requestedContainerPort, containerId));
                };

            context["net in service add port succeeds and returns a port"] = () =>
            {
                before =
                    () =>
                    {
                        mockNetInService.Setup(x => x.AddPort(It.IsAny<int>(), It.IsAny<string>())).Returns(8765);
                    };

                it["returns the port that the net in service returns"] = () =>
                {
                    JObject json = result.Content.ReadAsJson();
                    json["hostPort"].ToObject<int>().should_be(8765);
                };
            };
        }
    }
}