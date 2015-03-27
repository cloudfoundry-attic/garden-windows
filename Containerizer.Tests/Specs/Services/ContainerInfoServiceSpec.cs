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
using Containerizer.Services.Implementations;

namespace Containerizer.Tests.Specs.Services
{
    internal class ContainerInfoServiceSpec : nspec
    {
        private void describe_()
        {
            describe[Controller.Index] = () =>
            {
                Mock<IContainerService> mockContainerService = null;
                Mock<IContainerPropertyService> mockPropertyService = null;
                Mock<IExternalIP> mockExternalIP = null;
                string handle = "container-handle";
                ContainerInfoService service = null;
                ContainerInfoApiModel result = null;
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

                    service = new ContainerInfoService(mockContainerService.Object, mockPropertyService.Object, mockExternalIP.Object);
                };

                act = () =>
                {
                    result = service.GetInfoByHandle(handle);
                };

                it["returns info about the container"] = () =>
                {
                    var portMapping = result.MappedPorts[0];
                    portMapping.HostPort.should_be(expectedHostPort);
                    portMapping.ContainerPort.should_be(8080);
                };

                it["returns container properties"] = () =>
                {
                    var properties = result.Properties;
                    properties["Keymaster"].should_be("Gatekeeper");
                };

                it["returns the external ip address"] = () =>
                {
                    var extrernalIP = result.ExternalIP;
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
                        result.should_be_null();
                    };
                };
            };
        }
    }
}