#region

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using Moq;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class NetControllerSpec : nspec
    {
        private void describe_()
        {
            Mock<INetInService> mockNetInService = null;
            NetController netController = null;

            before = () =>
            {
                mockNetInService = new Mock<INetInService>();
                netController = new NetController(mockNetInService.Object)
                {
                    Configuration = new HttpConfiguration(),
                    Request = new HttpRequestMessage()
                };
            };

            describe[Controller.Create] = () =>
            {
                string containerId = null;
                int requestedContainerPort = 0;
                IHttpActionResult result = null;

                before = () =>
                {
                    containerId = Guid.NewGuid().ToString();
                    requestedContainerPort = 5432;
                    netController.Request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("hostPort", requestedContainerPort.ToString())
                    });
                };

                act = () =>
                {
                    result = netController.Create(containerId).Result;
                };

                it["returns a successful status code"] = () =>
                {
                    result.VerifiesSuccessfulStatusCode();
                };

                it["calls the net in service with its passed in parameters"] = () =>
                {
                    mockNetInService.Verify(x => x.AddPort(requestedContainerPort, containerId));
                };

                context["net in service add port succeeds and returns a port"] = () =>
                {
                    before = () =>
                    {
                        mockNetInService.Setup(x => x.AddPort(It.IsAny<int>(), It.IsAny<string>()))
                            .Returns(8765);
                    };

                    it["returns the port that the net in service returns"] = () =>
                    {
                        result.ReadContentAsJson()["hostPort"].ToObject<int>().should_be(8765);
                    };
                };
            };
        }
    }
}