using IronFoundry.Container;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Containerizer.Models;

namespace Containerizer.Controllers
{
    public class InfoController : ApiController
    {
        private readonly IContainerService containerService;

        public InfoController(IContainerService containerService)
        {
            this.containerService = containerService;
        }

        [Route("api/containers/{handle}/info")]
        public IHttpActionResult GetInfo(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return NotFound();
            }
            var rawInfo = container.GetInfo();
            var portMappings = new List<PortMappingApiModel>();
            foreach (int reservedPort in rawInfo.ReservedPorts)
            {
                portMappings.Add(new PortMappingApiModel { HostPort = reservedPort, ContainerPort = 8080 });
            }
            var apiResponse = new ContainerInfoApiModel { MappedPorts = portMappings };
            return Json(apiResponse);
        }
    }
}
