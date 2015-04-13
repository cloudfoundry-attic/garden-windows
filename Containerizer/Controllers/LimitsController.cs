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

    public class MemoryLimits
    {
        [JsonProperty("limit_in_bytes")]
        public ulong LimitInBytes { get; set; }
    }

    public class LimitsController : ApiController
    {
        private readonly IContainerService containerService;

        public LimitsController(IContainerService containerService)
        {
            this.containerService = containerService;
        }

        [Route("api/containers/{handle}/limit_memory")]
        [HttpPost]
        public IHttpActionResult LimitMemory(string handle, MemoryLimits limits)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return NotFound();
            }
            try
            {
                container.LimitMemory(limits.LimitInBytes);
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

        }
    }
}
