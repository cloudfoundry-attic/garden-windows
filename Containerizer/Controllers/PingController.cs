using System.Web.Http;

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