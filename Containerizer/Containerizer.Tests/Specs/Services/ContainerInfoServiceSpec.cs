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
using ContainerInfo = Containerizer.Models.ContainerInfo;

namespace Containerizer.Tests.Specs.Services
{
    internal class ContainerInfoServiceSpec : nspec
    {
        private void describe_()
        {
            Mock<IContainerService> mockContainerService = null;
            Mock<IContainer> mockContainer = null;
            string handle = "container-handle";
            ContainerInfoService service = null;
            Mock<IExternalIP> mockExternalIP = null;

            before = () =>
            {
                mockContainer = new Mock<IContainer>();
                mockContainerService = new Mock<IContainerService>();
                mockContainerService.Setup(x => x.GetContainerByHandle(handle)).Returns(mockContainer.Object);
                mockExternalIP = new Mock<IExternalIP>();
                service = new ContainerInfoService(mockContainerService.Object, mockExternalIP.Object);
            };

            describe["GetInfoByHandle"] = () =>
            {
                ContainerInfo result = null;
                int expectedHttpPort = 1234;
                int expectedSsshPort = 4567;
                string expectedExternalIP = "10.11.12.13";

                before = () =>
                {
                    mockContainer.Setup(x => x.GetInfo()).Returns(
                        new IronFrame.ContainerInfo
                        {
                            Properties = new Dictionary<string, string>
                            {
                                {"Keymaster", "Gatekeeper"},
                                {"ContainerPort:8080", "1234" },
                                {"ContainerPort:2222", "4567" }
                            }
                        });

                    mockExternalIP.Setup(x => x.ExternalIP()).Returns(expectedExternalIP);
                };

                act = () =>
                {
                    result = service.GetInfoByHandle(handle);
                };

                it["returns info about the container"] = () =>
                {
                    var portMapping = result.MappedPorts[0];
                    portMapping.HostPort.should_be(expectedHttpPort);
                    portMapping.ContainerPort.should_be(8080);
                    portMapping = result.MappedPorts[1];
                    portMapping.HostPort.should_be(expectedSsshPort);
                    portMapping.ContainerPort.should_be(2222);
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

            describe["GetMetricsByHandle"] = () =>
            {
                ContainerMetricsApiModel result = null;
                const ulong privateBytes = 7654;
                const ulong cpuUsage = 4321;
                const ulong diskUsage = 9641;

                before = () =>
                {
                    mockContainer.Setup(x => x.GetMetrics()).Returns(new IronFrame.ContainerMetrics()
                    {
                        MemoryStat = new ContainerMemoryStat
                        {
                            PrivateBytes = privateBytes
                        },
                        CpuStat = new ContainerCpuStat
                        {
                            TotalProcessorTime = new TimeSpan(0, 0, 0, 0, (int) cpuUsage)
                        }
                    });

                    mockContainer.Setup(x => x.CurrentDiskUsage()).Returns(diskUsage);
                };

                act = () => result = service.GetMetricsByHandle(handle);

                it["returns memory metrics about the container"] = () =>
                {
                    result.MemoryStat.TotalBytesUsed.should_be(privateBytes);
                };

                it["returns cpu usage metrics in nanoseconds"] = () =>
                {
                    result.CPUStat.Usage.should_be(cpuUsage*1000000);
                };

                it["returns disk usage metrics about the container"] = () =>
                {
                    result.DiskStat.ExclusiveBytesUsed.should_be(diskUsage);
                    result.DiskStat.TotalBytesUsed.should_be(diskUsage);
                };

                context["when the container does not exist"] = () =>
                {
                    before = () => mockContainerService.Setup(x => x.GetContainerByHandle(handle)).Returns(null as IContainer);

                    it["returns not found"] = () =>
                    {
                        result.should_be_null();
                    };
                };
            };
        }
    }
}