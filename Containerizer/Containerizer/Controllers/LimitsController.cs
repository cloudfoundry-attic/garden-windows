#region

using IronFrame;
using Newtonsoft.Json;
using System.Web.Http;


#endregion

namespace Containerizer.Controllers
{

    public class MemoryLimits
    {
        [JsonProperty("limit_in_bytes")]
        public ulong? LimitInBytes { get; set; }
    }

    public class CpuLimits
    {
        [JsonProperty("limit_in_shares")]
        public int? Weight { get; set; }
    }

    public class DiskLimits
    {
        [JsonProperty("byte_hard")]
        public ulong? ByteHard { get; set; }
    }

    public class LimitsController : ApiController
    {
        private readonly IContainerService containerService;

        public LimitsController(IContainerService containerService)
        {
            this.containerService = containerService;
        }

        [Route("api/containers/{handle}/memory_limit")]
        [HttpPost]
        public IHttpActionResult LimitMemory(string handle, MemoryLimits limits)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return NotFound();
            }

            if (limits.LimitInBytes != null) container.LimitMemory(limits.LimitInBytes.Value);
            return Ok();
        }

        [Route("api/containers/{handle}/memory_limit")]
        [HttpGet]
        public IHttpActionResult CurrentMemoryLimit(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return NotFound();
            }
            var limit = container.CurrentMemoryLimit();
            return Json(new MemoryLimits { LimitInBytes = limit });
        }

        [Route("api/containers/{handle}/cpu_limit")]
        [HttpPost]
        public IHttpActionResult LimitCpu(string handle, CpuLimits limits)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return NotFound();
            }

            if (limits.Weight != null) container.LimitCpu(limits.Weight.Value);
            return Ok();
        }


        [Route("api/containers/{handle}/cpu_limit")]
        [HttpGet]
        public IHttpActionResult CurrentCpuLimit(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return NotFound();
            }

            var limit = container.CurrentCpuLimit();
            return Json(new CpuLimits() { Weight = limit });
        }

        [Route("api/containers/{handle}/disk_limit")]
        [HttpPost]
        public IHttpActionResult LimitDisk(string handle, DiskLimits limits)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return NotFound();
            }

            if (limits.ByteHard != null) container.LimitDisk(limits.ByteHard.Value);
            return Ok();
        }

        [Route("api/containers/{handle}/disk_limit")]
        [HttpGet]
        public IHttpActionResult CurrentDiskLimit(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return NotFound();
            }

            var limit = container.CurrentDiskLimit();
            return Json(new DiskLimits() { ByteHard = limit });
        }
    }
}
