#region

using System.Collections.Specialized;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Containerizer.Services.Interfaces;
using Newtonsoft.Json;
using IronFrame;
using System;

#endregion

namespace Containerizer.Controllers
{
    public class NetInRequest
    {
        public int HostPort { get; set; }
        public int ContainerPort { get; set; }
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

        [Route("api/containers/{handle}/net/in")]
        [HttpPost]
        public IHttpActionResult Create(string handle, NetInRequest request)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return NotFound(); 
            }
            try
            {
                var returnedPort = container.ReservePort(request.HostPort);
                container.SetProperty("ContainerPort:" + request.ContainerPort.ToString(), returnedPort.ToString() );
                return Json(new NetInResponse { HostPort = returnedPort });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

        }
    }
}
