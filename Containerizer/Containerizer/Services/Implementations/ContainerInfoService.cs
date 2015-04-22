using Containerizer.Models;
using Containerizer.Services.Interfaces;
using IronFrame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containerizer.Services.Implementations
{
    public class ContainerInfoService : IContainerInfoService
    {
        private readonly IContainerService containerService;
        private readonly IExternalIP externalIP;

        public ContainerInfoService(IContainerService containerService, IExternalIP externalIP)
        {
            this.containerService = containerService;
            this.externalIP = externalIP;
        }

        public ContainerInfoApiModel GetInfoByHandle(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return null;
            }

            var rawInfo = container.GetInfo();
            var portMappings = new List<PortMappingApiModel>();
            foreach (int reservedPort in rawInfo.ReservedPorts)
            {
                portMappings.Add(new PortMappingApiModel { HostPort = reservedPort, ContainerPort = 8080 });
            }

            return new ContainerInfoApiModel
            {
                MappedPorts = portMappings,
                Properties = rawInfo.Properties,
                ExternalIP = externalIP.ExternalIP(),
            };
        }
    }
}
