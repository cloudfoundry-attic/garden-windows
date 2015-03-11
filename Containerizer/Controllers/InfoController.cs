using IronFoundry.Container;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Containerizer.Models;
using Containerizer.Services.Interfaces;
using System.IO;

namespace Containerizer.Controllers
{
    public class InfoController : ApiController
    {
        private readonly IContainerService containerService;
        private readonly IContainerPropertyService containerPropertyService;
        private readonly IExternalIP externalIP;


        public InfoController(IContainerService containerService, IContainerPropertyService containerPropertyService, IExternalIP externalIP)
        {
            this.containerService = containerService;
            this.containerPropertyService = containerPropertyService;
            this.externalIP = externalIP;
        }

        [Route("api/containers/{handle}/info")]
        public IHttpActionResult GetInfo(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return NotFound();
            }

            var properties = containerPropertyService.GetProperties(container);

            var rawInfo = container.GetInfo();
            var portMappings = new List<PortMappingApiModel>();
            foreach (int reservedPort in rawInfo.ReservedPorts)
            {
                portMappings.Add(new PortMappingApiModel { HostPort = reservedPort, ContainerPort = 8080 });
            }
            var apiResponse = new ContainerInfoApiModel
            {
                MappedPorts = portMappings,
                Properties = properties,
                ExternalIP = externalIP.ExternalIP(),
            };
            return Json(apiResponse);
        }
    }
}
