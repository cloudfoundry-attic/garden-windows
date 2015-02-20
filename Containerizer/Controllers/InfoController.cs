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
        private readonly IContainerPropertyService propertyService;


        public InfoController(IContainerService containerService, IContainerPropertyService propertyService)
        {
            this.containerService = containerService;
            this.propertyService = propertyService;
        }

        [Route("api/containers/{handle}/info")]
        public IHttpActionResult GetInfo(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return NotFound();
            }

            var properties = propertyService.GetProperties(container);

            var rawInfo = container.GetInfo();
            var portMappings = new List<PortMappingApiModel>();
            foreach (int reservedPort in rawInfo.ReservedPorts)
            {
                portMappings.Add(new PortMappingApiModel { HostPort = reservedPort, ContainerPort = 8080 });
            }
            var apiResponse = new ContainerInfoApiModel
            {
                MappedPorts = portMappings,
                Properties = properties
            };
            return Json(apiResponse);
        }
    }
}
