#region

using System.Web.Http;

#endregion

namespace Containerizer.Controllers
{
    public class PingController : ApiController
    {
        [Route("api/ping")]
        [HttpGet]
        public IHttpActionResult Show()
        {
            return Json("OK");
        }
    }
}