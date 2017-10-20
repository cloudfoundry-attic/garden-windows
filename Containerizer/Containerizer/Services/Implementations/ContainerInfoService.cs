using Containerizer.Controllers;
using Containerizer.Models;
using Containerizer.Services.Interfaces;
using IronFrame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContainerInfo = Containerizer.Models.ContainerInfo;

namespace Containerizer.Services.Implementations
{
    public class ContainerInfoService : IContainerInfoService
    {
        private readonly IContainerService containerService;
        private readonly IOptions options;

        public ContainerInfoService(IContainerService containerService, IOptions options)
        {
            this.containerService = containerService;
            this.options = options;
        }

        public ContainerInfo GetInfoByHandle(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return null;
            }

            var rawInfo = container.GetInfo();
            var portMappings = rawInfo.Properties.Where(x => x.Key.Contains("ContainerPort:")).
                Select(x => new PortMappingApiModel
                {
                    ContainerPort = int.Parse(x.Key.Split(':')[1]),
                    HostPort = int.Parse(x.Value),
                });

            return new ContainerInfo
            {
                MappedPorts = portMappings.ToList(),
                Properties = rawInfo.Properties,
                ExternalIP = options.MachineIp,
                ContainerIP = options.MachineIp,
            };
        }

        public ContainerMetricsApiModel GetMetricsByHandle(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return null;
            }

            var metrics = container.GetMetrics();

            var diskUsage = container.CurrentDiskUsage();
            return new ContainerMetricsApiModel
            {
                MemoryStat = new ContainerMemoryStatApiModel
                {
                   TotalBytesUsed = metrics.MemoryStat.PrivateBytes
                },
                CPUStat = new ContainerCPUStatApiModel
                {
                    Usage = (ulong)metrics.CpuStat.TotalProcessorTime.Ticks * 100
                },
                DiskStat = new ContainerDiskApiModel
                {
                    TotalBytesUsed = diskUsage,
                    ExclusiveBytesUsed = diskUsage
                }
            };
        }
    }
}
