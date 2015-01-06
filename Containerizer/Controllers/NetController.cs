#region

using System.Collections.Specialized;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Containerizer.Services.Interfaces;
using Newtonsoft.Json;

#endregion

namespace Containerizer.Controllers
{
    public class NetInResponse
    {
        [JsonProperty("hostPort")]
        public int HostPort { get; set; }
    }

    public class NetController : ApiController
    {
        private readonly INetInService netInService;


        public NetController(INetInService netInService)
        {
            this.netInService = netInService;
        }

        [Route("api/containers/{id}/net/in")]
        [HttpPost]
        public async Task<IHttpActionResult> Create(string id)
        {
            NameValueCollection formData = await Request.Content.ReadAsFormDataAsync();

            int hostPort = int.Parse(formData.Get("hostPort"));
            hostPort = netInService.AddPort(hostPort, id);
            return Json(new NetInResponse {HostPort = hostPort});
        }
    }
}