using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;

namespace Containerizer.Controllers
{
    public class PingController : ApiController
    {
        [Route("api/ping")]
        [HttpGet]
        public IHttpActionResult Ping()
        {
            return Json("OK");
        }
    }
}