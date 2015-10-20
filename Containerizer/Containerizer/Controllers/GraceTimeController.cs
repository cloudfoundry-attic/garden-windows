#region

using IronFrame;
using Newtonsoft.Json;
using System.Web.Http;


#endregion

namespace Containerizer.Controllers
{

    public class GraceTime
    {
        [JsonProperty("grace_time")]
        public long? GraceTimeInNanoSeconds { get; set; }
    }

    public class GraceTimeController : ApiController
    {
        private readonly IContainerService containerService;

        public GraceTimeController(IContainerService containerService)
        {
            this.containerService = containerService;
        }

        [Route("api/containers/{handle}/grace_time")]
        [HttpPost]
        public IHttpActionResult SetGraceTime(string handle, GraceTime graceTime)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return NotFound();
            }

            if (graceTime.GraceTimeInNanoSeconds != null)
            {
                container.SetProperty("GraceTime", graceTime.GraceTimeInNanoSeconds.Value.ToString());
            }
            else
            {
                container.SetProperty("GraceTime", null);
            }

            return Ok();
        }

        [Route("api/containers/{handle}/grace_time")]
        [HttpGet]
        public IHttpActionResult GetGraceTime(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return NotFound();
            }

            var graceTime = container.GetProperty("GraceTime");
            if (string.IsNullOrEmpty(graceTime))
            {
                return Json(new GraceTime { });
            }

            return Json(new GraceTime { GraceTimeInNanoSeconds = (long?)long.Parse(graceTime) });
        }
     
    }
}
