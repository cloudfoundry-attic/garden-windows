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
using IronFoundry.Container;
using Containerizer.Services.Interfaces;

namespace Containerizer.Tests.Specs.Controllers
{
    class InfoControllerSpec : nspec
    {
        private void describe_()
        {
            describe[Controller.Index] = () =>
            {
                Mock<IContainerService> mockContainerService = null;
                Mock<IContainerPropertyService> mockPropertyService = null;
                Mock<IExternalIP> mockExternalIP = null; 
                string handle = "container-handle";
                InfoController controller = null;
                IHttpActionResult result = null;
                int expectedHostPort = 1337;
                string expectedExternalIP = "10.11.12.13";

                before = () =>
                {
                    var mockContainer = new Mock<IContainer>();
                    mockContainer.Setup(x => x.GetInfo()).Returns(
                        new ContainerInfo
                        {
                            ReservedPorts = new List<int> { expectedHostPort },
                        });

                    mockContainerService = new Mock<IContainerService>();
                    mockContainerService.Setup(x => x.GetContainerByHandle(handle))
                        .Returns(mockContainer.Object);

                    mockPropertyService = new Mock<IContainerPropertyService>();
                    mockPropertyService.Setup(x => x.GetProperties(mockContainer.Object)).Returns(new Dictionary<string, string>
                    {
                        {"Keymaster", "Gatekeeper"}
                    });

                    mockExternalIP = new Mock<IExternalIP>();
                    mockExternalIP.Setup(x => x.ExternalIP()).Returns(expectedExternalIP);

                    controller = new InfoController(mockContainerService.Object, mockPropertyService.Object, mockExternalIP.Object);
                };

                act = () =>
                {
                    result = controller.GetInfo(handle);
                };

                it["returns info about the container"] = () =>
                {
                    var jsonResult = result.should_cast_to<JsonResult<ContainerInfoApiModel>>();
                    var portMapping = jsonResult.Content.MappedPorts[0];
                    portMapping.HostPort.should_be(expectedHostPort);
                    portMapping.ContainerPort.should_be(8080);
                };

                it["returns container properties"] = () =>
                {
                    var jsonResult = result.should_cast_to<JsonResult<ContainerInfoApiModel>>();
                    var properties = jsonResult.Content.Properties;
                    properties["Keymaster"].should_be("Gatekeeper");
                };

                it["returns the external ip address"] = () =>
                {
                    var jsonResult = result.should_cast_to<JsonResult<ContainerInfoApiModel>>();
                    var extrernalIP = jsonResult.Content.ExternalIP;
                    extrernalIP.should_be(expectedExternalIP);
                };

                context["when the container does not exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainerService.Setup(x => x.GetContainerByHandle(handle))
                       .Returns((IContainer)null);
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