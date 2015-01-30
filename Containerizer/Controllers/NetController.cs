#region

using System.Collections.Specialized;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Containerizer.Services.Interfaces;
using Newtonsoft.Json;
using IronFoundry.Container;
using System;

#endregion

namespace Containerizer.Controllers
{
    public class NetInRequest
    {
        public int HostPort { get; set; }
    }

    public class NetInResponse
    {
        [JsonProperty("hostPort")]
        public int HostPort { get; set; }
    }

    public class NetController : ApiController
    {
        private readonly IContainerService containerService;

        public NetController(IContainerService containerService)
        {
            this.containerService = containerService;
        }

        [Route("api/containers/{id}/net/in")]
        [HttpPost]
        public IHttpActionResult Create(string id, NetInRequest request)
        {
            var container = containerService.GetContainerByHandle(id);
            if (container == null)
            {
                return NotFound(); 
            }
            try
            {
                var returnedPort = container.ReservePort(request.HostPort);
                return Json(new NetInResponse { HostPort = returnedPort });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}