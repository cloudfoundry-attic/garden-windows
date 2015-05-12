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
            var portMappings = rawInfo.Properties.Where(x => x.Key.Contains("ContainerPort:")).                Select(x => new PortMappingApiModel
            {
                ContainerPort = int.Parse(x.Key.Split(':')[1]),
                HostPort = int.Parse(x.Value),
            });

            return new ContainerInfoApiModel
            {
                MappedPorts = portMappings.ToList(),
                Properties = rawInfo.Properties,
                ExternalIP = externalIP.ExternalIP(),
            };
        }
    }
}
