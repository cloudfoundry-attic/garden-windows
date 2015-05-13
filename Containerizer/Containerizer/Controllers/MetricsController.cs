#region

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Containerizer.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using IronFrame;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace Containerizer.Controllers
{
    public class MetricsController : ApiController
    {
        private readonly IContainerInfoService containerInfoService;

        public MetricsController(IContainerInfoService containerInfoService)
        {
            this.containerInfoService = containerInfoService;
        }

        [Route("api/containers/{handle}/metrics")]
        [HttpGet]
        public IHttpActionResult Show(string handle)
        {
            var info = containerInfoService.GetMetricsByHandle(handle);
            if (info == null)
            {
                return ResponseMessage(Request.CreateResponse(System.Net.HttpStatusCode.NotFound, string.Format("container does not exist: {0}", handle)));
            }
            return Json(info);
        }
    }
}