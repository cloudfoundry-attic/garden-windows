#region

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Containerizer.Facades;
using Containerizer.Services.Implementations;
using Microsoft.Web.WebSockets;
using Containerizer.Services.Interfaces;

#endregion

namespace Containerizer.Controllers
{
    public class RunController : ApiController
    {
        IContainerPathService pathService;
        public RunController(IContainerPathService pathService)
        {
            this.pathService = pathService;
        }

        [Route("api/containers/{id}/run")]
        [HttpGet]
        public Task<HttpResponseMessage> Index(string id)
        {
            HttpContext.Current.AcceptWebSocketRequest(new ContainerProcessHandler(id, pathService, new ProcessFacade()));
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.SwitchingProtocols);
            return Task.FromResult(response);
        }
    }
}